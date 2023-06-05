<template>
	<div class="border">
		<div
			class="p-2 border-bottom d-flex align-items-center justify-content-between"
			:class="{
				'bg-light border-gray': themeValue === 'vs-light',
				'bg-dark border-dark': themeValue === 'vs-dark',
			}"
		>
			<div>
				<b-button
					@click="showDocumentation"
					icon="book"
					v-b-tooltip="'Documentation'"
					iconClass="mr-1"
					size="sm"
					v-if="documentation"
				/>
				<b-button
					class="mr-1"
					v-b-tooltip="'Theme'"
					@click="themeValue = themeValue == 'vs-dark' ? 'vs-light' : 'vs-dark'"
					size="sm"
				>
					<i class="fa-solid fa-sun fa-fw" v-if="themeValue === 'vs-dark'" />
					<i class="fa-solid fa-moon fa-fw" v-else />
				</b-button>
				<b-button
					class="mr-1"
					v-b-tooltip="'Decrease Font Size'"
					size="sm"
					@click="fontSizeValue--"
				>
					<i class="fa-solid fa-minus fa-fw" />
				</b-button>
				<b-button
					class="mr-1"
					v-b-tooltip="'Increase Font Size'"
					@click="fontSizeValue++"
					size="sm"
				>
					<i class="fa-solid fa-plus fa-fw" />
				</b-button>
			</div>
			<div class="px-2 text-muted">
				<span class="mr-1">Language {{ language }}</span>
				<span class="mr-1">Theme {{ themeValue }}</span>
				<span>Font Size: {{ fontSizeValue }}</span>
			</div>
		</div>
		<div>
			<template v-if="compact">
				<b-button
					v-if="compact"
					class="btn btn-sm btn-outline-secondary"
					@click="toggle"
					:icon="'edit'"
					iconClass="'mr-1'"
					:text="'Edit'"
				/>
				<span
					class="text-muted text-monospace ml-2"
					v-if="value && value.length"
				>
					{{ value.substring(0, 100) }}...
				</span>
				<b-modal
					v-if="compact"
					title="Editor"
					v-model="show"
					hide-footer
					:enableFullScreen="true"
					@full-screen-modal-change="changeModalScreenMode($event)"
				>
					<MonacoEditor
						width="100%"
						:height="
							// enableFullScreenMode mixin'den geliyor
							enableFullScreenMode ? fullScreenModalHeight : height
						"
						:value="value"
						:theme="themeValue"
						:language="language"
						:readOnly="readOnly"
						:options="editorOptions"
						:intellisense="intellisense"
						@change="onChange"
					></MonacoEditor>
				</b-modal>
			</template>
			<MonacoEditor
				v-else
				width="100%"
				:height="
					// üst componentten parametre olarak geliyor
					enableFullScreen ? fullScreenModalHeight : height
				"
				:value="value"
				:theme="themeValue"
				:readOnly="readOnly"
				:language="language"
				:options="editorOptions"
				:intellisense="intellisense"
				@change="onChange"
			></MonacoEditor>
		</div>
	</div>
</template>

<script>
const fontSizeDefaultValue = 14;
import MonacoEditor from "./MonacoEditor.vue";

export default {
	name: "code-editor",
	components: { MonacoEditor },
	props: {
		documentation: {
			type: String,
			required: false,
		},
		language: {
			type: String,
			required: false,
			default: "csharp",
		},
		theme: {
			type: String,
			required: false,
			default: "vs-dark",
		},
		value: {
			type: String,
			default: "",
		},
		height: {
			type: String,
			default: "60vh",
		},
		options: {
			type: Object,
			default() {
				return {};
			},
		},
		compact: {
			type: Boolean,
			default: false,
		},
		intellisense: {
			type: Array,
			default: () => [],
		},
		readOnly: {
			type: Boolean,
			default: false,
		},
		fontSize: {
			type: Number,
			default: fontSizeDefaultValue,
		},
		enableFullScreen: {
			type: Boolean,
			default: false,
		},
	},
	data() {
		return {
			editor: null,
			editorId: "",
			show: false,
			themeData: this.theme,
			fontSizeData: this.fontSize,
			showDocumentationModal: false,
			documentationContext: "",
			documentationLoaded: false,
			fullScreenModalHeight: "80vh",
		};
	},
	computed: {
		editorOptions() {
			const defaultOptions = {
				automaticLayout: true,
				codeLens: false,
				minimap: {
					enabled: false,
				},
				wordWrap: "on",
				readOnly: this.readOnly,
				fontSize: this.fontSizeValue,
			};

			let all = Object.assign(defaultOptions, this.options);
			return all;
		},
		themeValue: {
			set(value) {
				this.themeData = value;
				this.setStorage("editor-theme", value);
			},
			get() {
				const value = this.getFromStorage("editor-theme", this.themeData);
				return value;
			},
		},
		fontSizeValue: {
			set(value) {
				this.fontSizeData = value;
				this.setStorage("editor-font-size", `${value}`);
			},
			get() {
				const value = this.getFromStorage(
					"editor-font-size",
					this.fontSizeData
				);
				return value;
			},
		},
	},
	methods: {
		async showDocumentation() {
			this.documentationLoaded = false;
			if (!this.documentation) {
				return;
			}

			if (this.documentationContext) {
				this.showDocumentationModal = true;
				this.documentationLoaded = true;
				return;
			}

			const result = await fetch(this.documentation);
			const content = await result.text();

			this.documentationContext = content;
			this.documentationLoaded = true;
			this.showDocumentationModal = true;
		},
		setStorage(key, value) {
			if (!localStorage) return;
			localStorage.setItem(key, `${value}`);
		},
		getFromStorage(key, defaultValue) {
			if (!localStorage) return defaultValue;
			const value = localStorage.getItem(key);
			if (value) {
				return value;
			}
			return defaultValue;
		},
		onChange(value) {
			this.$emit("input", value);
		},
		toggle() {
			this.show = !this.show;
		},
	},
};
</script>
