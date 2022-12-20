import axios from "axios";
import router from "../router";

export const HTTP = axios.create({
  headers: {
    "Content-Type": "application/json",
    "X-Requested-With": "XMLHttpRequest"
  }
});

HTTP.interceptors.response.use(
  response => response,
  error => {
    if (axios.isCancel(error)) return new Promise(() => { });
    let statusCode = error.response.status;
    if (statusCode === 401) {
      window.location.href = `/sign-in`;
      return Promise.reject(error);
    }

    if (statusCode === 403) {
      router.push({
        name: "Forbidden"
      });

      return Promise.resolve(error.response);
    }

    return Promise.resolve(error.response);
  }
);
