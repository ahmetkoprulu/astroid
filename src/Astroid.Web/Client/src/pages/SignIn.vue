<template>
	<div class="login--container p-3">
		<div class="mb-4">
			<div class="text-center">
				<img alt="Astroid Logo" src="../assets/logo.png" class="mx-auto" />
				<div class="h3-light">Sign In</div>
			</div>
		</div>
		<div>
			<b-form @submit.prevent>
				<b-form-group class="mb-3" label="Email">
					<b-form-input v-model="form.email" type="email"></b-form-input>
				</b-form-group>
				<b-form-group class="mb-4">
					<div slot="label" class="d-flex justify-content-between">
						<label>Password</label>
						<a href="/">Forgot?</a>
					</div>
					<b-form-input v-model="form.password" type="password"></b-form-input>
				</b-form-group>
				<b-button class="w-100 mb-4" variant="primary" @click="signIn">
					Sign In
				</b-button>
				<div class="d-flex justify-content-between mb-5">
					<div>
						<input type="checkbox" v-model="form.rememberMe" />
						<span className="pl-2" htmlFor="exampleCheck1">
							Stay signed in
						</span>
					</div>
					<a href="/">Need help?</a>
				</div>
				<div class="w-100 p-auto text-center">
					<div><a href="/sign-up">Create an Account</a></div>
				</div>
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
				email: null,
				password: null,
				rememberMe: false,
			},
		};
	},
	methods: {
		async signIn() {
			try {
				await HomeService.signIn(this.form);
				this.$router.push({ name: "market-list" });
			} catch (err) {
				console.error(err);
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

.hr-line {
	margin: 100px;
	border: 0;
	height: 1px;
	background-image: linear-gradient(
		to right,
		rgba(0, 0, 0, 0),
		rgba(0, 0, 0, 0.75),
		rgba(0, 0, 0, 0)
	);
	margin: 0px;
}
</style>
