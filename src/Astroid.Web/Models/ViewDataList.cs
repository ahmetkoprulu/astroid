namespace Astroid.Web
{
	public class MPViewDataList<T> where T : class
	{
		public List<T> Data { get; set; }
		public int CurrentPage { get; set; }
		public int PageCount { get; set; }
		public int TotalItemCount { get; set; }
		public string? Message { get; set; }
		public bool IsSuccess { get; set; }
		public int ItemPerPage { get; set; }

		public List<MPFilterValue> Filters { get; set; }

		public MPViewDataList()
		{
			Data = new List<T>();
			CurrentPage = 1;
			ItemPerPage = 10;
		}

		public MPMViewDataList ForJson(Func<T, object> func) => new MPMViewDataList
		{
			Data = Data?.Select(func).ToList(),
			CurrentPage = CurrentPage,
			Message = Message,
			ItemPerPage = ItemPerPage,
			IsSuccess = IsSuccess,
			PageCount = PageCount,
			TotalItemCount = TotalItemCount
		};
	}

	public class MPMViewDataList : MPViewDataList<object>
	{
	}

	public class MPFilterValue
	{
		public string Column { get; set; }
		public object Value { get; set; }
		public MPEFilterOperator Operator { get; set; }

		public MPFilterValue() { }

		public MPFilterValue(string column = null, object value = null, MPEFilterOperator opr = MPEFilterOperator.Equal)
		{
			Column = column;
			Value = value;
			Operator = opr;
		}
	}

	public enum MPEFilterOperator
	{
		SystemDefault = 0,
		Equal = 1,
		NotEqual = 2,
		GreaterThan = 3,
		GreaterThanAndEqual = 4,
		LessThan = 5,
		LessThanAndEqual = 6,
		In = 7,
		NotIn = 8,
		BeginsWith = 9,
		EndsWith = 10,
		Contains = 11,
		Empty = 12,
		NotEmpty = 13,
	}
}
