const defaultDelay = 5000;
const toastPosition = "b-toaster-top-right";
const variants = {
  success: "success",
  danger: "danger",
  primary: "primary"
};

const createOptions = (title, variant, noCloseButton, id) => {
  let options = {
    id: id,
    noCloseButton: noCloseButton,
    solid: false,
    appendToast: true,
    isStatus: true,
    autoHideDelay: defaultDelay,
    variant: variant,
    toaster: toastPosition
  };
  if (title) {
    options.title = title;
  }
  return options;
};

const createBody = (that, message, variant, noTitle, options) => {
  const h = that.$createElement;

  let properties = {
    class: ["fas", "fa-circle-info"],
    style: { color: "#2563EB" }
  };

  if (variant === variants.success) {
    properties.class = ["fas", "fa-circle-check"];
    properties.style.color = "#16A34A";
  } else if (variant === variants.danger) {
    properties.class = ["fas", "fa-circle-exclamation"];
    properties.style.color = "#DC2626";
  }

  if (noTitle) {
    const btnClose = h(
      "a",
      {
        href: "#",
        on: { click: () => that.$bvToast.hide(options.id) },
        class: ["ml-2", "mt-1", "cursor-pointer", "p-4"]
      },
      [
        h("i", {
          class: ["fas", "fa-xmark"],
          style: { color: "#DDD" }
        })
      ]
    );

    const msgBody = h(
      "p",
      { class: ["text-left", "p-4", "mb-0", "mt-1", "flex-grow-1"] },
      [h("i", properties), h("span", { class: ["ml-2"] }, message)]
    );
    return h("div", { class: ["d-flex"] }, [msgBody, btnClose]);
  }

  const msgBody = h("p", { class: ["text-left", "mb-0"] }, [
    h("i", properties),
    h("span", { class: ["ml-2"] }, message)
  ]);
  return msgBody;
};

const createTitle = (that, title) => {
  const h = that.$createElement;
  return h(
    "div",
    {
      class: ["d-flex", "flex-grow-1", "align-items-baseline", "mr-2"]
    },
    [
      h("span", { class: "mr-2" }, title),
      h("small", { class: "ml-auto text-italics" }, "")
    ]
  );
};

const createToast = (that, message, options) => {
  that.$bvToast.toast(
    [
      createBody(
        that,
        message,
        options.variant,
        options.title === null || options.title === undefined,
        options
      )
    ],
    options
  );
};

const toast = (that, title, message, variant) => {
  let messageBody = message || title;

  let hasTitle = false;
  if (title) hasTitle = true;

  const options = createOptions(
    hasTitle ? createTitle(that, title) : null,
    variant,
    !hasTitle
  );
  createToast(that, messageBody, options);
};

export default {
  install(Vue) {
    Vue.prototype.$errorToast = function (title, message) {
      toast(this, title, message, variants.danger);
    };

    Vue.prototype.$successToast = function (title, message) {
      toast(this, title, message, variants.success);
    };

    Vue.prototype.$infoToast = function (title, message) {
      toast(this, title, message, variants.primary);
    };
  }
};
