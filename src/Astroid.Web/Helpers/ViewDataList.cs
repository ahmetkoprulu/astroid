using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mapster;

namespace Astroid.Web.Helpers
{
	public static class ViewDataListHelper
	{
		public static MPViewDataList<T> ViewDataList<T>(
					this IQueryable<T> data,
					MPViewDataList<T> model)
					where T : class => ViewDataListAsync<T>(data, model).GetAwaiter().GetResult();

		public static async Task<MPViewDataList<T>> ViewDataListAsync<T>(
					this IQueryable<T> data,
					MPViewDataList<T> model)
					where T : class
		{
			var queryable = data;

			if (model.Filters != null && model.Filters.Any())
			{
				foreach (var filter in model.Filters)
				{
					try
					{
						var lambda = GetLambda<T>(filter);

						if (lambda == null) continue;

						queryable = (IOrderedQueryable<T>)queryable.Where(lambda);
					}
					catch (Exception ex)
					{
						var ss = ex;
					}
				}
			}

			if (model.ItemPerPage <= 0) model.ItemPerPage = 10;

			if (model.CurrentPage <= 0) model.CurrentPage = 1;

			model.TotalItemCount = queryable.Count();
			model.PageCount = model.TotalItemCount / model.ItemPerPage;

			if (model.TotalItemCount % model.ItemPerPage > 0) model.PageCount++;
			if (model.CurrentPage > model.PageCount) model.CurrentPage = model.PageCount;

			var cp = (model.CurrentPage - 1);

			if (cp < 0) cp = 0;

			queryable = queryable.Skip(cp * model.ItemPerPage).Take(model.ItemPerPage);
			if (queryable is IAsyncEnumerable<T>)
			{
				model.Data = await queryable.ToListAsync();
			}
			else
			{
				model.Data = queryable.ToList();
			}

			model.IsSuccess = true;

			return model;
		}

		public static async Task<MPViewDataList<TTarget>> ViewDataListAsync<TSource, TTarget>(
					this IQueryable<TSource> data,
					MPViewDataList<TTarget> model)
					where TTarget : class
		{
			TypeAdapterConfig.GlobalSettings.Default
				.PreserveReference(true)
				.MaxDepth(3);

			var queryable = data.ProjectToType<TTarget>().AsQueryable();

			if (model.ItemPerPage <= 0) model.ItemPerPage = 10;

			if (model.CurrentPage <= 0) model.CurrentPage = 1;

			model.TotalItemCount = queryable.Count();
			model.PageCount = model.TotalItemCount / model.ItemPerPage;

			if (model.TotalItemCount % model.ItemPerPage > 0) model.PageCount++;
			if (model.CurrentPage > model.PageCount) model.CurrentPage = model.PageCount;

			var cp = (model.CurrentPage - 1);

			if (cp < 0) cp = 0;

			queryable = queryable.Skip(cp * model.ItemPerPage).Take(model.ItemPerPage);
			if (queryable is IAsyncEnumerable<TSource>)
			{
				model.Data = await queryable.ToListAsync();
			}
			else
			{
				model.Data = queryable.ToList();
			}

			model.IsSuccess = true;

			return model;
		}

		public static MPViewDataList<T> ViewDataList<T>(
			this DbSet<T> set,
			MPViewDataList<T> model
			) where T : class => ViewDataList(set.AsQueryable(), model);

		private static Expression<Func<T, bool>> GetLambda<T>(MPFilterValue filter)
		{
			var parameter = Expression.Parameter(typeof(T), "x");
			//var property = Expression.Property(parameter, filter.Column);

			Expression body = parameter;
			foreach (var column in filter.Column.Split('.'))
			{
				body = Expression.PropertyOrField(body, column);
			}

			ConstantExpression target;
			try
			{
				target = Expression.Constant(TypeDescriptor.GetConverter(body.Type).ConvertFrom(filter.Value));
			}
			catch
			{
				return null;
			}

			var compType = body.Type.GetMethod("Equals", new[] { target.Type });
			Expression<Func<T, bool>> lambda;

			if (body.Type.IsEnum)
			{
				var enumType = body.Type.GetEnumUnderlyingType();
				compType = enumType.GetMethod("Equals", new[] { enumType });
				target = Expression.Constant(TypeDescriptor.GetConverter(enumType).ConvertFrom(filter.Value));

				var filterProperty = Expression.Convert(body, enumType);
				var method = Expression.Call(filterProperty, compType, target);
				lambda = Expression.Lambda<Func<T, bool>>(method, parameter);
			}
			else
			{
				MethodCallExpression method;
				if (body.Type == typeof(Guid) || body.Type == typeof(Guid?))
				{
					var convertedExpression = Expression.Convert(body, typeof(Guid));
					compType = typeof(Guid).GetMethod("Equals", new[] { target.Type });
					method = Expression.Call(convertedExpression, compType, target);
					lambda = Expression.Lambda<Func<T, bool>>(method, parameter);
				}
				else if (body.Type == typeof(string))
				{
					compType = body.Type.GetMethod("IndexOf", new[] { target.Type, typeof(StringComparison) });
					method = Expression.Call(body, compType, target, Expression.Constant(StringComparison.OrdinalIgnoreCase));
					lambda = Expression.Lambda<Func<T, bool>>(
						Expression.AndAlso(
							Expression.NotEqual(body, Expression.Constant(null)),
							Expression.GreaterThanOrEqual(method, Expression.Constant(0))
						),
						parameter
					);
				}
				else
				{
					method = Expression.Call(body, compType, target);
					lambda = Expression.Lambda<Func<T, bool>>(method, parameter);
				}
			}

			return lambda;
		}
	}
}
