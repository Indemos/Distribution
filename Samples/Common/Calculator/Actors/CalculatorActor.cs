using Distribution.AttributeSpace;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common
{
  /// <summary>
  /// An example of an actor performing calculations and tracking state 
  /// </summary>
  public class CalculatorActor
  {
    protected double _response = 0;
    protected IList<string> _operations = new List<string>();

    [Processor]
    public virtual Task<OperationResponse> Increment(IncrementMessage message)
    {
      _response += message.Input;

      SaveOperation($"+{message.Input}");

      var response = new OperationResponse
      {
        Value = message.Input,
        Operation = nameof(Increment)
      };

      return Task.FromResult(response);
    }

    [Processor]
    public virtual Task<OperationResponse> Decrement(DecrementMessage message)
    {
      _response -= message.Input;

      SaveOperation($"-{message.Input}");

      var response = new OperationResponse
      {
        Value = message.Input,
        Operation = nameof(Decrement)
      };

      return Task.FromResult(response);
    }

    [Processor]
    public virtual Task<OperationResponse> Summary(SummaryMessage message)
    {
      var response = new OperationResponse
      {
        Value = _response,
        Operation = nameof(Summary)
      };

      return Task.FromResult(response);
    }

    protected virtual void SaveOperation(string operation) => _operations.Add(operation);
  }
}
