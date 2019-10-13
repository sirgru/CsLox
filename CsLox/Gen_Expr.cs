using System;
using System.Collections.Generic;

namespace CsLox {

interface Visitor_Expr<R> {
	R Visit_AssignExpr(AssignExpr expr);
	R Visit_BinaryExpr(BinaryExpr expr);
	R Visit_CallExpr(CallExpr expr);
	R Visit_GetExpr(GetExpr expr);
	R Visit_SetExpr(SetExpr expr);
	R Visit_SuperExpr(SuperExpr expr);
	R Visit_ThisExpr(ThisExpr expr);
	R Visit_GroupingExpr(GroupingExpr expr);
	R Visit_LogicalExpr(LogicalExpr expr);
	R Visit_LiteralExpr(LiteralExpr expr);
	R Visit_UnaryExpr(UnaryExpr expr);
	R Visit_VariableExpr(VariableExpr expr);
}

abstract class Expr {
	public abstract R Accept<R>(Visitor_Expr<R> visitor);
}

class AssignExpr : Expr {
	public readonly Token name;
	public readonly Expr value;

	public AssignExpr (Token name,Expr value) {
		this.name = name;
		this.value = value;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_AssignExpr(this);
	}
}

class BinaryExpr : Expr {
	public readonly Expr left;
	public readonly Token op;
	public readonly Expr right;

	public BinaryExpr (Expr left,Token op,Expr right) {
		this.left = left;
		this.op = op;
		this.right = right;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_BinaryExpr(this);
	}
}

class CallExpr : Expr {
	public readonly Expr callee;
	public readonly Token paren;
	public readonly List<Expr> arguments;

	public CallExpr (Expr callee,Token paren,List<Expr> arguments) {
		this.callee = callee;
		this.paren = paren;
		this.arguments = arguments;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_CallExpr(this);
	}
}

class GetExpr : Expr {
	public readonly Expr obj;
	public readonly Token name;

	public GetExpr (Expr obj,Token name) {
		this.obj = obj;
		this.name = name;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_GetExpr(this);
	}
}

class SetExpr : Expr {
	public readonly Expr obj;
	public readonly Token name;
	public readonly Expr value;

	public SetExpr (Expr obj,Token name,Expr value) {
		this.obj = obj;
		this.name = name;
		this.value = value;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_SetExpr(this);
	}
}

class SuperExpr : Expr {
	public readonly Token keyword;
	public readonly Token method;

	public SuperExpr (Token keyword,Token method) {
		this.keyword = keyword;
		this.method = method;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_SuperExpr(this);
	}
}

class ThisExpr : Expr {
	public readonly Token keyword;

	public ThisExpr (Token keyword) {
		this.keyword = keyword;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_ThisExpr(this);
	}
}

class GroupingExpr : Expr {
	public readonly Expr expression;

	public GroupingExpr (Expr expression) {
		this.expression = expression;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_GroupingExpr(this);
	}
}

class LogicalExpr : Expr {
	public readonly Expr left;
	public readonly Token op;
	public readonly Expr right;

	public LogicalExpr (Expr left,Token op,Expr right) {
		this.left = left;
		this.op = op;
		this.right = right;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_LogicalExpr(this);
	}
}

class LiteralExpr : Expr {
	public readonly Object value;

	public LiteralExpr (Object value) {
		this.value = value;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_LiteralExpr(this);
	}
}

class UnaryExpr : Expr {
	public readonly Token op;
	public readonly Expr right;

	public UnaryExpr (Token op,Expr right) {
		this.op = op;
		this.right = right;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_UnaryExpr(this);
	}
}

class VariableExpr : Expr {
	public readonly Token name;

	public VariableExpr (Token name) {
		this.name = name;
	}

	public override R Accept<R>(Visitor_Expr<R> visitor) {
		return visitor.Visit_VariableExpr(this);
	}
}

}
