<template>
	<b-modal
		ref="editorModal"
		title="Computation Method"
		:size="showCodeEditor ? 'xl' : 'md'"
		ok-only
	>
		<b-form-group label="Order Book Depth">
			<b-form-input
				class="w-50"
				type="number"
				v-model="settings.orderBookDepth"
			/>
		</b-form-group>

		<b-form-group label="Code" v-if="settings.computationMethod == 2">
			<code class="code code-top">
				<span class="text-primary"> public <i>decimal</i> </span>
				<strong> ComputeEntryPoint </strong> (
				<i class="text-secondary">List&lt;OrderBookEntry&gt; entries </i>)
				<strong>{</strong>
			</code>
			<CodeEditor v-model="settings.code" />
			<code class="code code-bottom">}</code>
		</b-form-group>
	</b-modal>
</template>

<script>
import CodeEditor from "@/components/shared/code-editor/CodeEditor.vue";

export default {
	props: {
		settings: {
			type: Object,
			required: true,
		},
	},
	computed: {
		showCodeEditor() {
			return this.settings.computationMethod == 2;
		},
	},
	methods: {
		show() {
			this.$refs.editorModal.show();
		},
		hide() {
			this.$refs.editorModal.hide();
		},
	},
	components: {
		CodeEditor,
	},
};
</script>
<style scoped>
.code {
	width: 100%;
	font-size: 11pt;
	display: block;
	border-radius: 0px;
	background-color: #eee;
	border-left: 1px solid #ddd;
	border-right: 1px solid #ddd;
}
.code-top {
	border-top: 1px solid #ddd;
}
.code-bottom {
	border-bottom: 1px solid #ddd;
}
</style>
