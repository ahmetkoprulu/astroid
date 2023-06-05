<template>
	<div class="monaco_editor_container" :style="style"></div>
</template>

<script>
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
let binded = false;

export default {
	name: "MonacoEditor",
	props: {
		diffEditor: { type: Boolean, default: false },
		width: { type: [String, Number], default: "100%" },
		height: { type: [String, Number], default: "35vh" },
		original: String,
		value: String,
		language: { type: String, default: "javascript" },
		theme: { type: String, default: "vs" },
		options: {
			type: Object,
			default() {
				return {};
			},
		},
		editorMounted: { type: Function, default: () => {} },
		editorBeforeMount: { type: Function, default: () => {} },
		intellisense: { type: Array, default: () => [] },
	},
	watch: {
		options: {
			deep: true,
			handler(options) {
				this.editor && this.editor.updateOptions(options);
			},
		},
		value() {
			this.editor &&
				this.value !== this._getValue() &&
				this._setValue(this.value);
		},
		language() {
			if (!this.editor) return;
			if (this.diffEditor) {
				const { original, modified } = this.editor.getModel();
				monaco.editor.setModelLanguage(original, this.language);
				monaco.editor.setModelLanguage(modified, this.language);
			} else {
				monaco.editor.setModelLanguage(this.editor.getModel(), this.language);
			}
		},
		theme() {
			this.editor && monaco.editor.setTheme(this.theme);
		},
		style() {
			this.editor &&
				this.$nextTick(() => {
					this.editor.layout();
				});
		},
	},

	computed: {
		style() {
			return {
				width: !/^\d+$/.test(this.width) ? this.width : `${this.width}px`,
				height: !/^\d+$/.test(this.height) ? this.height : `${this.height}px`,
			};
		},
	},

	mounted() {
		this.initMonaco();
	},

	beforeDestroy() {
		this.editor && this.editor.dispose();
	},

	methods: {
		installResizeWatcher(el, fn, interval) {
			let offset = { width: el.offsetWidth, height: el.offsetHeight };
			setInterval(() => {
				let newOffset = {
					width: el.offsetWidth,
					height: el.offsetHeight,
				};
				if (
					offset.height !== newOffset.height ||
					offset.width !== newOffset.width
				) {
					offset = newOffset;
					fn();
				}
			}, interval);
		},
		initMonaco() {
			const { value, language, theme, options } = this;
			Object.assign(options, this._editorBeforeMount());
			this.editor = monaco.editor[
				this.diffEditor ? "createDiffEditor" : "create"
			](this.$el, {
				value: value,
				language: language,
				theme: theme,
				...options,
			});
			this.diffEditor && this._setModel(this.value, this.original);
			this._editorMounted(this.editor);
			const editor = this.editor;
			this.installResizeWatcher(this.$el, () => editor.layout(), 100);
		},

		_createDependencyProposals(range) {
			return this.intellisense.map((item) => {
				let kind = null;
				if (item.kind) {
					kind = monaco.languages.CompletionItemKind[item.kind];
				} else {
					kind = monaco.languages.CompletionItemKind.Property;
				}
				return {
					range: range,
					label: item.label,
					insertText: item.text,
					insertTextRules: item.rule,
					documentation: item.documentation,
					kind: kind,
				};
			});
		},

		_getEditor() {
			if (!this.editor) return null;
			return this.diffEditor ? this.editor.modifiedEditor : this.editor;
		},

		_setModel(value, original) {
			const { language } = this;
			const originalModel = monaco.editor.createModel(original, language);
			const modifiedModel = monaco.editor.createModel(value, language);
			this.editor.setModel({
				original: originalModel,
				modified: modifiedModel,
			});
		},

		_setValue(value) {
			let editor = this._getEditor();
			if (editor) return editor.setValue(value);
		},

		_getValue() {
			let editor = this._getEditor();
			if (!editor) return "";
			return editor.getValue();
		},

		_editorBeforeMount() {
			const options = this.editorBeforeMount(monaco);
			return options || {};
		},

		_editorMounted(editor) {
			this.editorMounted(editor, monaco);
			if (this.diffEditor) {
				editor.onDidUpdateDiff((event) => {
					const value = this._getValue();
					this._emitChange(value, event);
				});
			} else {
				editor.onDidChangeModelContent((event) => {
					const value = this._getValue();
					this._emitChange(value, event);
				});
			}

			let $this = this;
			if (binded) return;
			binded = true;
			monaco.languages.registerCompletionItemProvider(this.language, {
				provideCompletionItems: function (model, position) {
					// var textUntilPosition = model.getValueInRange({
					// 	startLineNumber: 1,
					// 	startColumn: 1,
					// 	endLineNumber: position.lineNumber,
					// 	endColumn: position.column
					// });
					// var match = textUntilPosition.match(
					// 	/"dependencies"\s*:\s*\{\s*("[^"]*"\s*:\s*"[^"]*"\s*,\s*)*([^"]*)?$/
					// );
					// if (!match) {
					// 	return { suggestions: [] };
					// }
					var word = model.getWordUntilPosition(position);
					var range = {
						startLineNumber: position.lineNumber,
						endLineNumber: position.lineNumber,
						startColumn: word.startColumn,
						endColumn: word.endColumn,
					};
					return {
						suggestions: $this._createDependencyProposals(range),
					};
				},
			});

			setTimeout(() => {
				$this.editor.updateOptions($this.options);
			}, 3000);
		},

		_emitChange(value, event) {
			this.$emit("change", value, event);
			this.$emit("input", value);
		},
	},
};
</script>
