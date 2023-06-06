<template>
	<b-modal
		ref="modal"
		title="Test Computation Method"
		size="md"
		@hidden="hidden"
	>
		<ValidationObserver ref="form">
			<v-validated-input label="Ticker">
				<b-form-input class="w-50" type="text" v-model="ticker" />
			</v-validated-input>
		</ValidationObserver>
		<template #modal-footer>
			<b-button
				variant="primary"
				class="mr-2"
				:disabled="isTesting"
				@click="test"
			>
				Test
			</b-button>
		</template>
		<div v-if="showResults">
			<b-form-group label="Long"> {{ results.long }} </b-form-group>
			<b-form-group label="Short"> {{ results.short }} </b-form-group>
		</div>
	</b-modal>
</template>

<script>
import Service from "@/services/bots.js";

export default {
	props: {
		bot: {
			type: Object,
			required: true,
		},
	},
	data() {
		return {
			ticker: "",
			showResults: false,
			isTesting: false,
			results: {
				long: 0,
				short: 0,
			},
		};
	},
	methods: {
		show() {
			this.$refs.modal.show();
		},
		hide() {
			this.$refs.modal.hide();
		},
		async test() {
			var isValid = await this.$refs.form.validate();
			if (!isValid) return;

			this.isTesting = true;
			try {
				const result = await Service.testComputationMethod(
					this.ticker,
					this.bot
				);
				if (!result.data.success) {
					this.$errorToast("Test Computation Method", result.data.message);
					this.isTesting = false;

					return;
				}

				this.showResults = true;
				this.results = result.data.data;
			} catch (e) {
				this.$errorToast("Test Computation Method", e.message);
			}

			this.isTesting = false;
		},
		hidden() {
			this.ticker = "";
			this.showResults = false;
			this.isTesting = false;
			this.results = {};
		},
	},
};
</script>

<style></style>
