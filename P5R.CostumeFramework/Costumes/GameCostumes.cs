﻿using P5R.CostumeFramework.Models;
using System.Collections;

namespace P5R.CostumeFramework.Costumes;

internal class GameCostumes : IReadOnlyCollection<Costume>
{
    private readonly List<Costume> costumes = new();

    public GameCostumes()
    {
        var characters = Enum.GetValues<Character>();
        for (int currentSet = 0; currentSet < VirtualOutfitsSection.GAME_OUTFIT_SETS + VirtualOutfitsSection.MOD_OUTFIT_SETS; currentSet++)
        {
            foreach (var character in characters)
            {
                var itemId = 0x7010 + (currentSet * 10) + (int)character - 1;
                this.costumes.Add(new(character, itemId));
            }
        }
    }

    public int Count => this.costumes.Count;

    public IEnumerator<Costume> GetEnumerator() => this.costumes.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.costumes.GetEnumerator();
}