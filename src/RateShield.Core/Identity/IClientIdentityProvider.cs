namespace RateShield.Core.Identity;

public interface IClientIdentityProvider<TContext>
{
    //nts: not dependent of asp.net, hence I made generic
    ClientIdentity ResolveClient(TContext contex);
}
