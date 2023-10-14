const defaultDelay = 3000;

export default {
	install(Vue) {
		function toast(that, title, message, variant) {
			const h = that.$createElement;
			let icon = "check-circle";
			if (variant === "danger") {
				icon = "exclamation-triangle";
			} else if (variant === "info") {
				icon = "info-circle";
			}

			const titleElement = h(
				"div",
				{
					class: ["d-flex", "flex-grow-1", "align-items-baseline", "mr-2"]
				},
				[
					h("i", {
						class: `text-${variant} fa-solid fa-${icon} fa-fw mr-1`
					}),
					h("span", { class: `mr-2 text-dark` }, title),
					h("small", { class: "ml-auto text-italics" }, "")
				]
			);

			const messageElement = h("p", { class: ["text-left", "mb-0"] }, [
				h("span", { class: ["ml-1"] }, message)
			]);

			that.$root.$bvToast.toast([messageElement], {
				title: [titleElement],
				solid: false,
				appendToast: true,
				variant: variant,
				autoHideDelay: defaultDelay,
				isStatus: true,
				toaster: "b-toaster-bottom-right"
			});
		}

		Vue.prototype.$errorToast = function (title, message) {
			toast(this, title, message, "danger");
		};

		Vue.prototype.$successToast = function (title, message) {
			toast(this, title, message, "success");
		};

		Vue.prototype.$toast = function (title, message) {
			toast(this, title, message, "primary");
		};
	}
};
