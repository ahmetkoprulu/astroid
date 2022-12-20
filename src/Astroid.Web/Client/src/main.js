import Vue from 'vue'
import VueRouter from "vue-router";

import router from "./router/index";

import { BootstrapVue, IconsPlugin } from 'bootstrap-vue'

import 'bootstrap/dist/css/bootstrap.css'
import 'bootstrap-vue/dist/bootstrap-vue.css'
import "./css/default.css";

Vue.use(VueRouter)
Vue.use(BootstrapVue)
Vue.use(IconsPlugin)

Vue.config.productionTip = false

export { Vue, router }
