using System.Collections.Generic;

public class Sequence : Node
{
    private readonly List<Node> children;

    public Sequence(List<Node> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {
        foreach (Node child in children)
        {
            NodeState state = child.Evaluate();

            if(state == NodeState.Failure)
            {
                return NodeState.Failure;
            }

            if(state == NodeState.Running)
            {
                return NodeState.Running;
            }
        }

        return NodeState.Success;
    }
}