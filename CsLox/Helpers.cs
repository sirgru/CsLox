using System;

namespace CsLox {
	public static class Helpers {
		public static string ToText(this Object obj) {
			if(obj == null) return "nil";
			return obj.ToString();
		}
	}
}
