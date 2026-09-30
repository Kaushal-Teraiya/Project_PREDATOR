using System;

public class ConditionNode : Node
{
    private readonly Func<bool> condition;

    public ConditionNode(Func<bool> condition)
    {
        this.condition = condition;
    }

    public override NodeState Evaluate()
    {
        return condition()? NodeState.Success : NodeState.Failure;
    }
}