using System;
using System.Collections.Generic;

namespace CsLox {
	class Environment {
		public readonly Environment enclosing;
		private Dictionary<string, Object> _values = new Dictionary<string, object>();
		private int _depth = 0;

		public Environment() {
			enclosing = null;
		}

		public Environment(Environment enclosing) {
			this.enclosing = enclosing;
			_depth = enclosing._depth + 1;
		}

		internal void Define(string name, Object value) {
			_values.Add(name, value);
		}

		internal Object Get(Token nameToken) {
			if(nameToken.type != TokenType.Identifier) throw new Exception("Invalid token type treated as identifier.");

			string name = (string) nameToken.value;
			var hasKey = _values.TryGetValue(name, out Object value);

			if(hasKey) return value;

			if(enclosing != null) return enclosing.Get(nameToken);

			throw new RuntimeException(nameToken.line, nameToken.column, "Undefined variable '" + name + "'");
		}

		internal Object GetAt(int distance, String name) {
			var env = Ancestor(distance);
			return env._values[name];
		}
		Environment Ancestor(int distance) {
			Environment environment = this;
			for(int i = 0; i < distance; i++) {
				environment = environment.enclosing;
			}
			return environment;
		}

		internal void Assign(Token nameToken, Object value) {
			var name = (string)nameToken.value;
			if(_values.ContainsKey(name)) {
				_values[name] = value;
				return;
			}
			if(enclosing != null) {
				enclosing.Assign(nameToken, value);
				return;
			}
			throw new RuntimeException(nameToken.line, nameToken.column, "Undefined variable '" + name + "'.");
		}

		internal void AssignAt(int distance, Token nameToken, Object value) {
			var name = (string)nameToken.value;
			var v = Ancestor(distance)._values;
			if(!v.ContainsKey(name)) v.Add(name, value);
			else v[name] = value;
		}
	}
}
