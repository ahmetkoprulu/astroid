import Vue from 'vue'
import VueRouter from "vue-router";
import Consts from "@/core/consts";
import Helpers from "@/core/helpers";
import moment from "moment"
import router from "./router/index";

import { BootstrapVue, IconsPlugin } from 'bootstrap-vue'
import Alerts from "@/core/alert";
import Notifications from "@/core/notification";

import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-vue/dist/bootstrap-vue.css'
import "@riophae/vue-treeselect/dist/vue-treeselect.css";
import "./css/default.css";

Vue.use(VueRouter)
Vue.use(BootstrapVue)
Vue.use(IconsPlugin)
Vue.use(Notifications);
Vue.use(Alerts);

// Vee Validate Configurations
import {
	ValidationObserver,
	ValidationProvider,
	extend,
	setInteractionMode
} from "vee-validate";
import {
	min,
	required,
	email,
	regex,
	min_value
} from "vee-validate/dist/rules";

setInteractionMode("passive");

extend("required", required);
extend("min", min);
extend("email", email);
extend("regex", regex);
extend("min_value", min_value);

Vue.component("ValidationObserver", ValidationObserver);
Vue.component("ValidationProvider", ValidationProvider);

// Vue Tel Input
import VueTelInput from 'vue-tel-input';
import 'vue-tel-input/dist/vue-tel-input.css';
const options = {
	dropdownOptions: {
		showDialCodeInList: true,
		// showDialCodeInSelection: true,
		showSearchBox: true
	},
	inputOptions: {
		showDialCode: true,
	},
	autoDefaultCountry: false,
}
Vue.use(VueTelInput, options);

// Components
import PageHeader from "@/components/layout/PageHeader.vue";
import Table from "@/components/shared/table/Table.vue";
import Select from "@/components/shared/Select.vue";
import DynamicInput from "@/components/shared/DynamicInput.vue";
import Checkbox from "@/components/shared/Checkbox.vue";
import DateTime from "@/components/shared/Date/DateTime.vue";
import Dropdown from "@/components/shared/Dropdown/Dropdown.vue";
import DropdownItem from "@/components/shared/Dropdown/DropdownItem.vue";
import DropdownDivider from "@/components/shared/Dropdown/DropdownDivider.vue";
import RadioGroup from "@/components/shared/RadioGroup.vue";
import ValidatedInput from "@/components/shared/ValidatedInput.vue";
import Button from "@/components/shared/Button.vue";

Vue.component("page-header", PageHeader);
Vue.component("v-table", Table);
Vue.component("v-select", Select);
Vue.component("v-dynamic-input", DynamicInput);
Vue.component("v-checkbox", Checkbox);
Vue.component("v-datetime", DateTime);
Vue.component("v-dropdown", Dropdown);
Vue.component("v-dropdown-item", DropdownItem);
Vue.component("v-dropdown-divider", DropdownDivider);
Vue.component("v-radio-group", RadioGroup);
Vue.component("v-validated-input", ValidatedInput);
Vue.component("v-button", Button);

// Filters
Vue.filter("hideInvalidDate", function (value) {
	if (!value) return "";
	if (value === "01/01/0001 00:00") return "";
	return value;
});

Vue.filter("translate", function (value) {
	let pattern = /{{T:(.+)}}/;
	let isTranslationValue = pattern.test(value);

	if (isTranslationValue) {
		let key = pattern.exec(value);
		return Helpers.translate(key[1]);
	}
	return value;
});

Vue.filter("valid", function (value) {
	var date = moment.utc(value);
	var isValid = date.isValid() && date.year() > 1;
	return isValid;
});

Vue.filter("format", function (value, format = null, toLocal = true) {
	if (!format) {
		format = Consts.formattedDateTimeFormat;
	}
	if (toLocal) {
		const val = moment.utc(value);
		return val.local().format(format);
	}
	return moment(value).format(format);
});

Vue.filter("pretty", function (value, placeholder) {
	let isValid =
		value &&
		value !== "0001-01-01T00:00:00" &&
		value !== "9999-12-31T23:59:59.9999999";
	if (!isValid) return placeholder != null ? placeholder : "never";

	var date = moment.utc(value);
	if (date.isValid()) return date.fromNow();

	return placeholder != null ? placeholder : "never";
});

Vue.filter("suffix", function (value, suffix, seperator) {
	if (!seperator) {
		seperator = " ";
	}
	return `${value}${seperator}${suffix}`;
});

Vue.filter("prefix", function (value, prefix, seperator) {
	if (!seperator) {
		seperator = " ";
	}
	return `${prefix}${seperator}${value}`;
});

Vue.filter("capitalsplit", function (text, join = " ") {
	return text.match(/[A-Z][a-z]+/g).join(join);
});

Vue.filter("replaceEmptyGuid", t =>
	t != Consts.emptyGuid
		? t
		: `<small class="text-muted text-small">empty</small>`
);

Vue.filter("replaceEmptyValue", t =>
	t ? t : `<small class="text-muted">empty</small>`
);

Vue.filter("truncate", (v, n) => {
	if (v.length <= n) return v;
	return v.split("").slice(0, n - 3).join("") + "...";
});

//Prototypes
Vue.prototype.$consts = Consts;
Vue.prototype.$helpers = Helpers;

const user = Vue.observable({ user: {} });
Object.defineProperty(Vue.prototype, '$user', {
	get() { return user.user; },
	set(value) { user.user = value; }
});

const busy = Vue.observable({ busy: false });
Object.defineProperty(Vue.prototype, '$busy', {
	get() { return busy.busy; },
	set(value) { busy.busy = value; }
});

Vue.config.productionTip = false

export { Vue, router }
