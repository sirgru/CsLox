using System;
using System.Collections.Generic;

namespace CsLox {
	internal class Interpreter : Visitor_Expr<Object>, Visitor_Stmt<Void> {

		public Environment globals = new Environment();
		private Environment _environment;
		private Dictionary<Expr, int> _locals = new Dictionary<Expr, int>();

		public Interpreter() {
			_environment = globals;
		}

		internal string Interpret(List<Stmt> statements) {
			try {
				foreach(var stmt in statements) {
					Execute(stmt);
				}
				return null;
			} catch(RuntimeException ex) {
				return ex.message;
			}
		}

		internal void BindLocal(Expr expr, int distance) {
			var exprPrint = new AST_Printer().Print(expr);
			_locals.Add(expr, distance);
		}

		private void Execute(Stmt stmt) {
			stmt.Accept(this);
		}

		public Object Visit_LiteralExpr(LiteralExpr expr) {
			return expr.value;
		}

		public Object Visit_GroupingExpr(GroupingExpr expr) {
			return Evaluate(expr.expression);
		}

		private Object Evaluate(Expr expr) {
			return expr.Accept(this);
		}

		public Object Visit_UnaryExpr(UnaryExpr expr) {
			Object right = Evaluate(expr.right);

			switch(expr.op.type) {
				case TokenType.Bang: return !IsTruthy(right);
				case TokenType.Minus: return -(double)right;
			}
			throw new Exception("Unhandled case");
		}

		private bool IsTruthy(Object obj) {
			if(obj == null) return false;
			if(obj is bool) return (bool)obj;
			return true;
		}

		public Object Visit_BinaryExpr(BinaryExpr expr) {
			Object left = Evaluate(expr.left);
			Object right = Evaluate(expr.right);

			switch(expr.op.type) {
				case TokenType.Greater:
					CheckNumberOperands(expr.op, left, right);
					return (double)left > (double)right;

				case TokenType.GreaterEqual:
					return (double)left >= (double)right;

				case TokenType.Less:
					return (double)left < (double)right;

				case TokenType.LessEqual:
					return (double)left <= (double)right;

				case TokenType.Minus:
					CheckNumberOperand(expr.op, right);
					return (double)left - (double)right;

				case TokenType.Plus:
					if(left is Double && right is Double) {
						return (double)left + (double)right;
					}

					if(left is String && right is String) {
						return (String)left + (String)right;
					}

					throw new RuntimeException(expr.op.line, expr.op.column, "Operands must be two numbers or two strings.");

				case TokenType.Slash: return (double)left / (double)right;

				case TokenType.Star: return (double)left * (double)right;

				case TokenType.BangEqual: return !IsEqual(left, right);

				case TokenType.EqualEqual: return IsEqual(left, right);
			}
			throw new Exception("Unhandled case");
		}

		private bool IsEqual(Object a, Object b) {
			// nil is only equal to nil.               
			if(a == null && b == null) return true;
			if(a == null) return false;

			return a.Equals(b);
		}

		private void CheckNumberOperand(Token op, Object operand) {
			if(operand is Double) return;
			throw new RuntimeException(op.line, op.column, "Operand must be a number.");
		}
		private void CheckNumberOperands(Token op, Object left, Object right) {
			if(left is Double && right is Double) return;
			throw new RuntimeException(op.line, op.column, "Operands must be a numbers.");
		}

		public Void Visit_ExpressionStmt(ExpressionStmt stmt) {
			Evaluate(stmt.expression);
			return null;
		}

		public Void Visit_PrintStmt(PrintStmt stmt) {
			Object value = Evaluate(stmt.expression);
			Console.WriteLine(value.ToText());
			return null;
		}

		public object Visit_VariableExpr(VariableExpr expr) {
			return LookUpVariable(expr.name, expr);
		}
		private Object LookUpVariable(Token name, Expr expr) {
			bool hasKey = _locals.TryGetValue(expr, out int distance);
			if(hasKey) {
				return _environment.GetAt(distance, (string)name.value);
			} else {
				return globals.Get(name);
			}
		}

		public Void Visit_VarStmt(VarStmt stmt) {
			Object value = null;
			if(stmt.initializer != null) {
				value = Evaluate(stmt.initializer);
			}
			_environment.Define((string)stmt.name.value, value);
			return null;
		}

		public object Visit_AssignExpr(AssignExpr expr) {
			Object value = Evaluate(expr.value);

			bool hasKey = _locals.TryGetValue(expr, out int distance);
			if(hasKey) {
				_environment.AssignAt(distance, expr.name, value);
			} else {
				globals.Assign(expr.name, value);
			}
			return value;
		}

		public Void Visit_BlockStmt(BlockStmt stmt) {
			ExecuteBlock(stmt.statements, new Environment(_environment));
			return null;
		}

		public void ExecuteBlock(List<Stmt> statements, Environment environment) {
			Environment previous = this._environment;
			try {
				this._environment = environment;

				foreach(var statement in statements) {
					Execute(statement);
				}
			} finally {
				this._environment = previous;
			}
		}

		public Void Visit_IfStmt(IfStmt stmt) {
			if(IsTruthy(Evaluate(stmt.condition))) {
				Execute(stmt.thenBranch);
			} else if(stmt.elseBranch != null) {
				Execute(stmt.elseBranch);
			}
			return null;
		}

		public object Visit_LogicalExpr(LogicalExpr expr) {
			Object left = Evaluate(expr.left);

			if(expr.op.type == TokenType.Or) {
				if(IsTruthy(left)) return left;
			} else {
				if(!IsTruthy(left)) return left;
			}

			return Evaluate(expr.right);
		}

		public Void Visit_WhileStmt(WhileStmt stmt) {
			while(IsTruthy(Evaluate(stmt.condition))) {
				Execute(stmt.body);
			}
			return null;
		}

		public object Visit_CallExpr(CallExpr expr) {
			Object callee = Evaluate(expr.callee);

			List<Object> arguments = new List<Object>();
			foreach(var argument in expr.arguments) {
				arguments.Add(Evaluate(argument));
			}

			if(!(callee is LoxCallable)) {
				throw new RuntimeException(expr.paren.line, expr.paren.column, "Can only call functions and classes.");
			}
			LoxCallable function = (LoxCallable)callee;
			if(arguments.Count != function.Arity) {
				throw new RuntimeException(expr.paren.line, expr.paren.column, "Expected " 
										 + function.Arity + " arguments but got " + arguments.Count + ".");
			}
			return function.Call(this, arguments);
		}

		public Void Visit_FunctionStmt(FunctionStmt stmt) {
			LoxFunction function = new LoxFunction(stmt, _environment);
			_environment.Define((string)stmt.name.value, function);
			return null;
		}

		public Void Visit_ReturnStmt(ReturnStmt stmt) {
			Object value = null;
			if(stmt.value != null) value = Evaluate(stmt.value);

			throw new Return(value);
		}

		public Void Visit_ClassStmt(ClassStmt stmt) {
			Object superclass = null;
			if(stmt.superclass != null) {
				superclass = Evaluate(stmt.superclass);
				if(!(superclass is LoxClass)) {
					throw new RuntimeException(stmt.superclass.name.line, stmt.superclass.name.column, "Superclass must be a class.");
				}
			}

			var name = (string)stmt.name.value;
			_environment.Define(name, null);

			if(stmt.superclass != null) {
				_environment = new Environment(_environment);
				_environment.Define("super", superclass);
			}

			Dictionary<String, LoxFunction> methods = new Dictionary<string, LoxFunction>();
			foreach(var method in stmt.methods) {
				LoxFunction function = new LoxFunction(method, _environment, name == "init");
				methods.Add((string)method.name.value, function);
			}

			LoxClass klass = new LoxClass((string)stmt.name.value, (LoxClass)superclass, methods);

			if(superclass != null) _environment = _environment.enclosing;

			_environment.Assign(stmt.name, klass);
			return null;
		}

		public Object Visit_GetExpr(GetExpr expr) {
			Object obj = Evaluate(expr.obj);
			if(obj is LoxInstance) {
				return ((LoxInstance)obj).Get(expr.name);
			}
			throw new RuntimeException(expr.name.line, expr.name.column, "Only instances have properties.");
		}

		public Object Visit_SetExpr(SetExpr expr) {
			Object obj = Evaluate(expr.obj);

			if(!(obj is LoxInstance)) {
				throw new RuntimeException(expr.name.line, expr.name.column, "Only instances have fields.");
			}

			Object value = Evaluate(expr.value);
			((LoxInstance)obj).Set(expr.name, value);
			return value;
		}

		public Object Visit_ThisExpr(ThisExpr expr) {
			return LookUpVariable(expr.keyword, expr);
		}

		public Object Visit_SuperExpr(SuperExpr expr) {
			int distance = _locals[expr];
			LoxClass superclass = (LoxClass)_environment.GetAt(distance, "super");

			// "this" is always one level nearer than "super"'s environment.
			LoxInstance obj = (LoxInstance)_environment.GetAt(distance - 1, "this");

			string name = (string)expr.method.value;
			LoxFunction method = superclass.FindMethod(name);
			if(method == null) {
				throw new RuntimeException(expr.method.line, expr.method.column, "Undefined property '" + name + "'.");
			}

			return method.Bind(obj);
		}
	}

	internal class LoxClass : LoxCallable {
		public readonly String name;
		public readonly LoxClass superclass;
		private Dictionary<string, LoxFunction> _methods;

		internal LoxClass(String name, LoxClass superclass, Dictionary<string, LoxFunction> methods) {
			this.name = name;
			this.superclass = superclass;
			_methods = methods;
		}

		public override String ToString() {
			return name;
		}

		public Object Call(Interpreter interpreter, List<Object> arguments) {
			LoxInstance instance = new LoxInstance(this);

			LoxFunction initializer = FindMethod("init");
			if(initializer != null) {
				initializer.Bind(instance).Call(interpreter, arguments);
			}

			return instance;
		}

		public int Arity {
			get {
				LoxFunction initializer = FindMethod("init");
				if(initializer == null) return 0;
				return initializer.Arity;
			}
		}

		public LoxFunction FindMethod(String name) {
			if(_methods.ContainsKey(name)) {
				return _methods[name];
			}

			if(superclass != null) {
				return superclass.FindMethod(name);
			}

			return null;
		}
	}

	internal class LoxInstance {
		private LoxClass _klass;
		private Dictionary<string, Object> _fields = new Dictionary<string, object>();

		public LoxInstance(LoxClass klass) {
			_klass = klass;
		}

		public override String ToString() {
			return _klass.name + " instance";
		}

		public Object Get(Token nameToken) {
			var name = (string)nameToken.value;
			var hasKey = _fields.TryGetValue(name, out Object value);
			if(hasKey) return value;

			LoxFunction method = _klass.FindMethod(name);
			if(method != null) return method.Bind(this);

			throw new RuntimeException(nameToken.line, nameToken.column, "Undefined property '" + name + "'.");
		}

		public void Set(Token name, Object value) {
			_fields.Add((string)name.value, value);
		}
	}

	internal class Void { }

	public class RuntimeException : Exception {
		public readonly string message;

		public RuntimeException(int line, int column, string message) {
			this.message = "Runtime exception on line " + line + " column " + column + " : " + message;
		}
	}

	interface LoxCallable {
		int Arity { get; }
		Object Call(Interpreter interpreter, List<Object> arguments);
	}

	class LoxFunction : LoxCallable {
		private FunctionStmt _declaration;
		private readonly Environment _closure;
		private readonly bool _isInitializer;

		public LoxFunction(FunctionStmt declaration, Environment closure, bool isInitializer = false) {
			_declaration = declaration;
			_closure = closure;
			_isInitializer = isInitializer;
		}

		public LoxFunction Bind(LoxInstance instance) {
			Environment environment = new Environment(_closure);
			environment.Define("this", instance);
			return new LoxFunction(_declaration, environment, _isInitializer);
		}

		public int Arity { get { return _declaration.parameters.Count; } }

		public Object Call(Interpreter interpreter, List<Object> arguments) {
			Environment environment = new Environment(_closure);
			for(int i = 0; i < _declaration.parameters.Count; i++) {
				environment.Define((string)_declaration.parameters[i].value, arguments[i]);
			}

			try {
				interpreter.ExecuteBlock(_declaration.body, environment);
			} catch(Return returnValue) {
				if(_isInitializer) return _closure.GetAt(0, "this");

				return returnValue.value;
			}

			if(_isInitializer) return _closure.GetAt(0, "this");
			return null;
		}

		public override string ToString() {
			return "<fn " + (string)_declaration.name.value + ">";
		}
	}

	class Return : Exception {
		public readonly Object value;

		public Return(Object value) {
			this.value = value;
		}
	}
}
