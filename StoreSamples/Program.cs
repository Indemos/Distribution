using Distribution.DomainSpace;
using ReactiveStore.ActionSpace;
using ReactiveStore.SelectorSpace;
using System;
using System.Threading.Tasks;

namespace ReactiveStore
{
  public class Program
  {
    /// <summary>
    /// Example with two node instances X and Y.
    /// Each node has its own state for A, B, C. 
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task Main(string[] args)
    {
      var storeX = "X";
      var storeY = "Y";
      var scene = new Scene();
      var response = new AccountSelector();

      scene.Subscribe<AccountSelector>(o => Console.WriteLine($"Transaction subscription for {o.Name} is {o.Amount}"));

      response = await scene.Send<AccountSelector>(storeX, new DepositAction { Name = "A", Amount = 100 });
      Console.WriteLine($"Deposit 100 to A");

      response = await scene.Send<AccountSelector>(storeX, new WithdrawAction { Name = "A", Amount = 50 });
      Console.WriteLine($"Withdraw 50 from A");

      response = await scene.Send<AccountSelector>(storeX, new DepositAction { Name = "B", Amount = 100 });
      Console.WriteLine($"Deposit 100 to B");

      response = await scene.Send<AccountSelector>(storeX, new DepositAction { Name = "A", Amount = 100 });
      Console.WriteLine($"Deposit 100 to A");

      response = await scene.Send<AccountSelector>(storeY, new DepositAction { Name = "C", Amount = 1000 });
      Console.WriteLine($"Deposit 1000 to C in store Y");

      var balanceA = await scene.Send<AccountStatusSelector>(storeX, new StatusAction { Name = "A" });
      Console.WriteLine($"Balance for A is {balanceA.Amount}");

      var balanceB = await scene.Send<AccountStatusSelector>(storeX, new StatusAction { Name = "B" });
      Console.WriteLine($"Balance for B is {balanceB.Amount}");

      var balanceCX = await scene.Send<AccountStatusSelector>(storeX, new StatusAction { Name = "C" });
      Console.WriteLine($"Balance for C in store X is {balanceCX.Amount}");

      var balanceCY = await scene.Send<AccountStatusSelector>(storeY, new StatusAction { Name = "C" });
      Console.WriteLine($"Balance for C in store Y is {balanceCY.Amount}");

      Console.ReadKey();
    }
  }
}
