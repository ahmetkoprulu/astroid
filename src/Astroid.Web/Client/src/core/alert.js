import VueSweetalert2 from 'vue-sweetalert2';
import 'sweetalert2/dist/sweetalert2.min.css';

export default {
	install(Vue) {
		Vue.use(VueSweetalert2);

		Vue.prototype.$alert = function (
			title,
			text,
			type = "info",
			onConfirm,
			confirmButtonText = "Confirm",
			confirmButtonColor = "#000",
			cancelButtonText = "No, cancel!"
		) {
			this.$swal({
				title: title,
				type: type,
				html: text,
				showCancelButton: true,
				focusConfirm: false,
				reverseButtons: true,
				confirmButtonColor: confirmButtonColor,
				confirmButtonText: confirmButtonText,
				cancelButtonText: cancelButtonText
			}).then(result => {
				if (result.value === true) {
					onConfirm();
				}
			});
		};

		Vue.prototype.$alert.remove = function (title, text, onConfirm, confirmButtonText = "<i class='fas fa-fw fa-trash'></i> Yes, delete it!") {
			Vue.prototype.$alert(
				title,
				text,
				"warning",
				onConfirm,
				confirmButtonText,
				"#d33"
			);
		};
	}
};
