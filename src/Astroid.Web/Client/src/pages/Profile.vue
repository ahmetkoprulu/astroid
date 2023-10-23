<template>
	<div>
		<page-header title="Profile" :actions="actions" />
		<ValidationObserver ref="form">
			<div class="col-lg-4 col-md-6 col-sm-12">
				<v-validated-input label="Name">
					<b-form-input v-model="profile.name" />
				</v-validated-input>
			</div>
			<div class="col-lg-4 col-md-6 col-sm-12">
				<v-validated-input label="Email">
					<b-form-input type="email" v-model="profile.email" />
				</v-validated-input>
			</div>
			<div class="col-lg-4 col-md-6 col-sm-12">
				<b-form-group label="Phone">
					<PhoneInput v-model="profile.phone" />
				</b-form-group>
			</div>
			<div class="col-lg-4 col-md-6 col-sm-12">
				<b-form-group label="Telegram Id">
					<b-form-input v-model="profile.telegramId" />
				</b-form-group>
			</div>
			<div class="col-lg-4 col-md-6 col-sm-12">
				<b-form-group label="Notification Preference">
					<v-select
						v-model="profile.channelPreference"
						:options="channelOptions"
						placeholder="Select a notification channel"
					>
						<div slot="value-label" slot-scope="{ node }">
							<i :class="`mr-2 ${node.raw.icon}`" />
							<span>{{ node.label }}</span>
						</div>
						<label slot="option-label" slot-scope="{ node }">
							<i :class="`mr-2 ${node.raw.icon}`" />
							<span>{{ node.label }}</span>
						</label>
					</v-select>
				</b-form-group>
			</div>
		</ValidationObserver>
	</div>
</template>
<script>
import PhoneInput from "@/components/shared/PhoneInput";
import Service from "@/services/users";
export default {
	data() {
		return {
			actions: [
				{
					title: "Save",
					event: () => this.save(),
					icon: "fas fa-check",
					variant: "primary",
				},
			],
			profile: {},
		};
	},
	computed: {
		channelOptions() {
			var a = Object.values(this.$consts.CHANNEL_TYPES).map((x) => ({
				id: x.id,
				label: x.title,
				icon: x.icon,
			}));
			console.log(a);
			return a;
		},
	},
	async created() {
		this.$busy = true;
		await this.get();
		this.$busy = false;
	},
	methods: {
		async get() {
			var response = await Service.getProfile();
			if (!response.data.success) return;

			this.profile = response.data.data;
		},
		async save() {
			var isValid = await this.$refs.form.validate();
			if (!isValid) return;

			var response = await Service.saveProfile(this.profile);
			if (!response.data.success) return;

			this.$user = this.$helpers.cloneObject(this.profile);
		},
	},
	components: {
		PhoneInput,
	},
};
</script>
