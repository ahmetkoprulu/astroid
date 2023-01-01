export default {
	generateNameField(name, exclude = "") {
		if (!exclude.includes("İ")) name = name.replace(/İ/g, "I");
		if (!exclude.includes("ı")) name = name.replace(/ı/g, "i");
		if (!exclude.includes("ğ")) name = name.replace(/ğ/g, "g");
		if (!exclude.includes("Ğ")) name = name.replace(/Ğ/g, "G");
		if (!exclude.includes("ü")) name = name.replace(/ü/g, "u");
		if (!exclude.includes("Ü")) name = name.replace(/Ü/g, "U");
		if (!exclude.includes("ş")) name = name.replace(/ş/g, "s");
		if (!exclude.includes("Ş")) name = name.replace(/Ş/g, "S");
		if (!exclude.includes("ö")) name = name.replace(/ö/g, "o");
		if (!exclude.includes("'")) name = name.replace(/'/g, "");
		if (!exclude.includes("^")) name = name.replace(/\^/g, "");
		if (!exclude.includes("Ö")) name = name.replace(/Ö/g, "O");
		if (!exclude.includes("ç")) name = name.replace(/ç/g, "c");
		if (!exclude.includes("Ç")) name = name.replace(/Ç/g, "C");
		if (!exclude.includes(" ")) name = name.replace(/ /g, "");
		if (!exclude.includes("<")) name = name.replace(/</g, "");
		if (!exclude.includes(">")) name = name.replace(/>/g, "");
		if (!exclude.includes('"')) name = name.replace(/"/g, "");
		if (!exclude.includes("é")) name = name.replace(/é/g, "");
		if (!exclude.includes("!")) name = name.replace(/!/g, "");
		if (!exclude.includes("'")) name = name.replace(/'/, "");
		if (!exclude.includes("£")) name = name.replace(/£/, "");
		if (!exclude.includes("^")) name = name.replace(/^/, "");
		if (!exclude.includes("#")) name = name.replace(/#/, "");
		if (!exclude.includes("$")) name = name.replace(/$/, "");
		if (!exclude.includes("+")) name = name.replace(/\+/g, "");
		if (!exclude.includes("%")) name = name.replace(/%/g, "");
		if (!exclude.includes("½")) name = name.replace(/½/g, "");
		if (!exclude.includes("&")) name = name.replace(/&/g, "");
		if (!exclude.includes("/")) name = name.replace(/\//g, "");
		if (!exclude.includes("{")) name = name.replace(/{/g, "");
		if (!exclude.includes("(")) name = name.replace(/\(/g, "");
		if (!exclude.includes("[")) name = name.replace(/\[/g, "");
		if (!exclude.includes(")")) name = name.replace(/\)/g, "");
		if (!exclude.includes("]")) name = name.replace(/]/g, "");
		if (!exclude.includes("=")) name = name.replace(/=/g, "");
		if (!exclude.includes("}")) name = name.replace(/}/g, "");
		if (!exclude.includes("?")) name = name.replace(/\?/g, "");
		if (!exclude.includes("*")) name = name.replace(/\*/g, "");
		if (!exclude.includes("@")) name = name.replace(/@/g, "");
		if (!exclude.includes("€")) name = name.replace(/€/g, "");
		if (!exclude.includes("~")) name = name.replace(/~/g, "");
		if (!exclude.includes("æ")) name = name.replace(/æ/g, "");
		if (!exclude.includes("ß")) name = name.replace(/ß/g, "");
		if (!exclude.includes(";")) name = name.replace(/;/g, "");
		if (!exclude.includes(",")) name = name.replace(/,/g, "");
		if (!exclude.includes("`")) name = name.replace(/`/g, "");
		if (!exclude.includes("|")) name = name.replace(/|/g, "");
		if (!exclude.includes(".")) name = name.replace(/\./g, "");
		if (!exclude.includes(":")) name = name.replace(/:/g, "");
		if (!exclude.includes("-")) name = name.replace(/-/g, "");
		if (!exclude.includes("--")) name = name.replace(/--/g, "");
		if (!exclude.includes("---")) name = name.replace(/---/g, "");
		if (!exclude.includes("----")) name = name.replace(/----/g, "");
		return name;
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
	editRegex(regex, indexChar, removeIndex, newChars) {
		let regexString = regex.toString();
		let editedRegex = this.stringSplice(
			regexString,
			regexString.indexOf(indexChar),
			removeIndex,
			newChars
		);
		return new RegExp(editedRegex.slice(1, -1));
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
	addTableFilter(filters, filterField, filterValue) {
		let filter = filters.find(x => x.Column === filterField);
		if (!filter) {
			filter = { Column: filterField, Value: filterValue };
			filters.push(filter);
		} else {
			filter.Value = filterValue;
		}
		if (!filterValue) {
			if (filter) {
				filters = filters.filter(x => x !== filter);
			}
		}
	},
	cloneObject(oldObject) {
		return JSON.parse(JSON.stringify(oldObject));
	}
};
