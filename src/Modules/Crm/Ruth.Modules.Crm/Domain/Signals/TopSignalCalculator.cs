using Ruth.Modules.Crm.Domain.People;

namespace Ruth.Modules.Crm.Domain.Signals;

/// <summary>Pure domain service recalculating the highest-priority active TopSignal on a <see cref="Person" />.</summary>
public static class TopSignalCalculator
{
    /// <summary>Resolves and applies the top active signal (Order ASC, SignalTypeId ASC) onto <paramref name="person" />.</summary>
    public static void Recalculate(
        Person person,
        IEnumerable<PersonSignal> personSignals,
        IReadOnlyDictionary<SignalTypeId, SignalType> signalTypesById,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(personSignals);
        ArgumentNullException.ThrowIfNull(signalTypesById);

        var top = personSignals
            .Where(s => s.IsActiveAt(nowUtc) && signalTypesById.ContainsKey(s.SignalTypeId))
            .Select(s => (Signal: s, Type: signalTypesById[s.SignalTypeId]))
            .OrderBy(static x => x.Type.Order)
            .ThenBy(static x => x.Type.Id.Value)
            .FirstOrDefault();

        if (top.Signal is null)
        {
            person.SetTopSignal(signalId: null, color: null, icon: null);
            return;
        }

        person.SetTopSignal(top.Signal.Id.Value, top.Type.SignalColor, top.Type.SignalIcon);
    }
}
