<template>
	<div class="login--container p-3">
		<div class="mb-4">
			<div class="text-center">
				<img alt="Vue logo" src="../assets/logo.png" class="mx-auto" />
				<div class="h3-light">Sign up</div>
			</div>
		</div>
		<div>
			<b-form @submit.prevent>
				<ValidationObserver ref="form">
					<v-validated-input class="mb-3" label="Name">
						<b-form-input v-model="form.name" type="text"></b-form-input>
					</v-validated-input>
					<v-validated-input class="mb-3" label="Email">
						<b-form-input v-model="form.email" type="email"></b-form-input>
					</v-validated-input>
					<v-validated-input class="mb-3" label="Password">
						<b-form-input
							v-model="form.password"
							type="password"
						></b-form-input>
					</v-validated-input>
					<v-validated-input
						class="mb-3"
						label="Confirm Password"
						rules="required|confirmed:Password"
					>
						<b-form-input
							v-model="form.confirmPassword"
							type="password"
						></b-form-input>
					</v-validated-input>
					<b-button class="w-100 mb-4 mt-4" variant="primary" @click="register">
						Sign Up
					</b-button>
					<div class="w-100 p-auto text-center">
						<div>
							<router-link :to="{ name: 'signIn' }">
								Already have an account?
							</router-link>
						</div>
					</div>
				</ValidationObserver>
			</b-form>
		</div>
	</div>
</template>

<script>
import HomeService from "../services/home";

export default {
	data() {
		return {
			form: {
				name: null,
				email: null,
				password: null,
				confirmPassword: null,
			},
		};
	},
	methods: {
		async register() {
			const isValid = await this.$refs.form.validate();
			if (!isValid) return;

			try {
				var response = await HomeService.signUp(this.form);
				if (!response.data.success) {
					this.$errorToast("Sign Up", response.data.message);
					return;
				}

				this.$router.push({ name: "signIn" });
			} catch (err) {
				this.$errorToast("Sign Up", response.data.message);
			}
		},
	},
};
</script>

<style scoped>
.login--container {
	min-width: 350px;
	min-height: 400px;
	position: absolute;
	top: 50%;
	left: 53%;
	transform: translate(-50%, -50%);
	padding: 10px;
}

.h3-light {
	font-size: 1.75rem;
	font-weight: 400;
}
</style>
