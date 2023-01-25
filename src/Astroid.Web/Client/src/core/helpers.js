export default {
	pascalCaseToTitleCase(text) {
		return text
			.replace(/([A-Z])/g, " $1")
			.replace(/^./, function (str) {
				return str.toUpperCase();
			});
	},
	incrementalValueForArray(array, value, propertyName = undefined) {
		const postfix =
			array.filter(e => e[propertyName].replace(/\s\d+$/, "") === value)
				.length || "";
		return `${value} ${postfix}`.trim();
	},
	stringSplice(text, index, removeIndex, newText) {
		return (
			text.slice(0, index) + newText + text.slice(index + Math.abs(removeIndex))
		);
	},
	debounce(fn, delay) {
		let timeout;
		return (...args) => {
			const context = this;
			clearTimeout(timeout);
			timeout = setTimeout(() => fn.apply(context, args), delay);
		};
	},
	guid() {
		function s4() {
			return Math.floor((1 + Math.random()) * 0x10000)
				.toString(16)
				.substring(1);
		}
		return (
			s4() +
			s4() +
			"-" +
			s4() +
			"-" +
			s4() +
			"-" +
			s4() +
			"-" +
			s4() +
			s4() +
			s4()
		);
	},
	cloneObject(oldObject) {
		return JSON.parse(JSON.stringify(oldObject));
	}
};
