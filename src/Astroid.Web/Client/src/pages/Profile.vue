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
