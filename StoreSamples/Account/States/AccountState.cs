using Distribution.AttributeSpace;
using ReactiveStore.ActionSpace;
using ReactiveStore.SelectorSpace;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReactiveStore.StoreSpace
{
  public class AccountState
  {
    protected Dictionary<string, double> _account = new();

    [Processor]
    public virtual Task<AccountStatusSelector> Deposit(StatusAction message)
    {
      return Task.FromResult(new AccountStatusSelector
      {
        Amount = _account.TryGetValue(message.Name, out var o) ? o : 0
      });
    }

    [Processor]
    public virtual Task<AccountSelector> Deposit(DepositAction message)
    {
      if (_account.ContainsKey(message.Name) is false)
      {
        _account[message.Name] = 0;
      }

      _account[message.Name] += message.Amount;

      return Task.FromResult(new AccountSelector
      {
        Name = message.Name,
        Amount = _account[message.Name]
      });
    }

    [Processor]
    public virtual Task<AccountSelector> Withdraw(WithdrawAction message)
    {
      if (_account.ContainsKey(message.Name) is false)
      {
        _account[message.Name] = 0;
      }

      _account[message.Name] -= message.Amount;

      return Task.FromResult(new AccountSelector
      {
        Name = message.Name,
        Amount = _account[message.Name]
      });
    }
  }
}
