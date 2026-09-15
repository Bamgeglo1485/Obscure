using System.Linq;

using Content.Shared.Vanilla.MODSuit;
using Content.Shared.Clothing;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Inventory.Events;

using Robust.Client.GameObjects;

using Robust.Shared.Utility;
using Robust.Shared.Network;
using Robust.Shared.Containers;

namespace Content.Client.Vanilla.MODSuit;

public sealed partial class ClientMODSuitSystem : SharedMODSuitSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IClientNetManager _net = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MODSuitModuleInserted>(OnModuleInserted);
        SubscribeNetworkEvent<MODSuitModuleRemoved>(OnModuleRemoved);

        SubscribeLocalEvent<MODSuitFrameComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals);
        SubscribeLocalEvent<MODSuitFrameVisualsComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<MODSuitFrameComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnModuleInserted(MODSuitModuleInserted ev)
    {
        var modSuit = GetEntity(ev.MODSuit);
        if (!TryComp<MODSuitFrameComponent>(modSuit, out var frame))
            return;

        UpdateFrameVisuals((modSuit, frame));
    }

    private void OnModuleRemoved(MODSuitModuleRemoved ev)
    {
        var modSuit = GetEntity(ev.MODSuit);
        if (!TryComp<MODSuitFrameComponent>(modSuit, out var frame))
            return;

        UpdateFrameVisuals((modSuit, frame));
    }

    private void OnAppearanceChange(Entity<MODSuitFrameComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (ent.Comp.RSIPath == null)
            return;

        var moduleIcons = new List<(string IconState, int Priority)>();

        if (_containerSystem.TryGetContainer(ent, ent.Comp.SlotId, out var container))
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (TryComp<MODSuitModuleComponent>(contained, out var moduleComp) &&
                    !string.IsNullOrEmpty(moduleComp.IconState))
                {
                    moduleIcons.Add((moduleComp.IconState, moduleComp.SpritePriority));
                }
            }
        }

        moduleIcons = moduleIcons.OrderByDescending(x => x.Priority).ToList();

        ClearModuleIcons(ent);

        for (var i = 0; i < moduleIcons.Count; i++)
        {
            var layerKey = $"module-icon-{i}";
            var iconState = moduleIcons[i].IconState;

            var entSprite = new Entity<SpriteComponent?>(ent.Owner, sprite);
            var specifier = new SpriteSpecifier.Rsi(ent.Comp.RSIPath.Value, iconState);

            var newIndex = _sprite.AddLayer(entSprite, specifier);
            _sprite.LayerMapSet(entSprite, layerKey, newIndex);
            _sprite.LayerSetVisible(entSprite, newIndex, true);
        }
    }

    private void OnGetEquipmentVisuals(Entity<MODSuitFrameComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (ent.Comp.RSIPath == null)
            return;

        var modules = new List<(EntityUid Entity, MODSuitModuleComponent Component)>();

        if (_containerSystem.TryGetContainer(ent, ent.Comp.SlotId, out var container))
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (TryComp<MODSuitModuleComponent>(contained, out var moduleComp) &&
                    !string.IsNullOrEmpty(moduleComp.EquippedState))
                {
                    modules.Add((contained, moduleComp));
                }
            }
        }

        if (modules.Count == 0)
            return;

        var sortedModules = modules.OrderBy(x => x.Component.SpritePriority).ToList();

        if (!TryComp<SpriteComponent>(args.Equipee, out var sprite))
            return;

        var slotLayerExists = _sprite.LayerMapTryGet((args.Equipee, sprite), args.Slot, out var baseIndex, false);

        if (!slotLayerExists)
            return;

        var layerDataList = new List<(string Key, PrototypeLayerData Data)>();

        for (var i = 0; i < sortedModules.Count; i++)
        {
            var (moduleEntity, moduleComp) = sortedModules[i];

            if (string.IsNullOrEmpty(moduleComp.EquippedState))
                continue;

            var layerKey = $"module-{i}";

            var layer = new PrototypeLayerData
            {
                RsiPath = ent.Comp.RSIPath.Value.ToString(),
                State = moduleComp.EquippedState,
                Visible = true
            };

            layerDataList.Add((layerKey, layer));
        }

        var entSprite = (args.Equipee, sprite);
        var baseKey = "module";
        var removeIndex = 0;
        while (true)
        {
            var layerKey = $"{baseKey}-{removeIndex}";
            if (!_sprite.LayerMapTryGet(entSprite, layerKey, out var layerIdx, false))
                break;

            _sprite.RemoveLayer(entSprite, layerIdx);
            removeIndex++;
        }

        var currentIndex = baseIndex + 1;
        foreach (var (key, data) in layerDataList)
        {
            _sprite.AddBlankLayer(entSprite, currentIndex);
            _sprite.LayerMapSet(entSprite, key, currentIndex);
            _sprite.LayerSetData(entSprite, currentIndex, data);

            currentIndex++;
        }
    }

    private void OnGotUnequipped(Entity<MODSuitFrameVisualsComponent> ent, ref GotUnequippedEvent args)
    {
        if (!TryComp<SpriteComponent>(args.EquipTarget, out var sprite))
            return;

        var entSprite = new Entity<SpriteComponent?>(args.EquipTarget, sprite);
        var baseKey = "module";
        var index = 0;

        while (true)
        {
            var layerKey = $"{baseKey}-{index}";
            if (!_sprite.LayerMapTryGet(entSprite, layerKey, out var layerIndex, false))
                break;

            _sprite.RemoveLayer(entSprite, layerIndex);
            index++;
        }
    }

    private void ClearModuleIcons(Entity<MODSuitFrameComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var entSprite = new Entity<SpriteComponent?>(ent.Owner, sprite);
        var baseKey = "module-icon";
        var index = 0;

        while (true)
        {
            var layerKey = $"{baseKey}-{index}";
            if (!_sprite.LayerMapTryGet(entSprite, layerKey, out var layerIndex, false))
                break;

            _sprite.RemoveLayer(entSprite, layerIndex);
            index++;
        }
    }

    private void UpdateFrameVisuals(Entity<MODSuitFrameComponent> ent)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            _sprite.ForceUpdate(ent);
        }
    }
}
