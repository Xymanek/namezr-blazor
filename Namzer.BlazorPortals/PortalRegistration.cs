using Microsoft.AspNetCore.Components;

namespace Namzer.BlazorPortals;

public class PortalRegistration
{
    private record PortalFragment(string Identifier, RenderFragment Fragment)
    {
        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }

    private Dictionary<string, HashSet<PortalFragment>> _portalFragments = new();
    private Dictionary<string, Action> _subscriptions = new();

    private class PortalSubscription : IDisposable
    {
        private readonly string _name;
        private readonly Dictionary<string, Action> _subscriptions;

        public PortalSubscription(string name, Action action, Dictionary<string, Action> subscriptions)
        {
            _name = name;
            _subscriptions = subscriptions;
            _subscriptions.Add(name, action);
        }

        public void Dispose()
        {
            _subscriptions.Remove(_name);
        }
    }

    public void AddToPortal(string name, string idenfifier, RenderFragment fragment)
    {
        if(_portalFragments.ContainsKey(name))
        {
            _portalFragments[name].Add(new PortalFragment(idenfifier, fragment));
        }
        else
        {
            _portalFragments.Add(name, new HashSet<PortalFragment>() { new (idenfifier, fragment) });
        }

        if (_subscriptions.TryGetValue(name, out var action))
        {
            action();
        }
    }

    public void RemoveFromPortal(string name, string identifier) 
    {
        if(_portalFragments.ContainsKey(name))
        {
            _portalFragments[name].RemoveWhere(x => x.Identifier== identifier);
        }

        if (_subscriptions.TryGetValue(name, out var action))
        {
            action();
        }
    }

    public IEnumerable<RenderFragment> GetRenderFragments(string name)
    {
        if(_portalFragments.TryGetValue(name, out var fragments))
        {
            return fragments.Select(x => x.Fragment);
        }

        return Enumerable.Empty<RenderFragment>();
    }


    public IDisposable Subscribe(string name, Action action)
    {
        //refactor this to not pass in the subscriptions dictionary? made more sense when class was static
        return new PortalSubscription(name, action, _subscriptions);
    }
}
