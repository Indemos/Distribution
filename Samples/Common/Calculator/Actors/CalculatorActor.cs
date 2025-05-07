using Distribution.Attributes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common
{
  /// <summary>
  /// An example of an actor performing calculations and tracking state 
  /// </summary>
  public class CalculatorActor
  {
    protected double response = 0;
    protected IList<string> operations = new List<string>();

    [Processor]
    public virtual Task<OperationResponse> Increment(IncrementMessage message)
    {
      this.response += message.Input;

      SaveOperation($"+{message.Input}");

      var response = new OperationResponse
      {
        Value = this.response,
        Operation = nameof(Increment)
      };

      return Task.FromResult(response);
    }

    [Processor]
    public virtual Task<OperationResponse> Decrement(DecrementMessage message)
    {
      this.response -= message.Input;

      SaveOperation($"-{message.Input}");

      var response = new OperationResponse
      {
        Value = this.response,
        Operation = nameof(Decrement)
      };

      return Task.FromResult(response);
    }

    [Processor]
    public virtual Task<OperationResponse> Summary(SummaryMessage message)
    {
      var response = new OperationResponse
      {
        Value = this.response,
        Operation = nameof(Summary)
      };

      return Task.FromResult(response);
    }

    protected virtual void SaveOperation(string operation) => operations.Add(operation);
  }
}
