using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ActTransitionPlaceholderArtLibrary",
    menuName = "Hex/Acts/Act Transition Placeholder Art Library")]
public sealed class HexActTransitionPlaceholderArtLibrary : ScriptableObject
{
    public Texture2D[] act1ToAct2 = Array.Empty<Texture2D>();
    public Texture2D[] act2ToAct3 = Array.Empty<Texture2D>();

    public Texture2D GetRandomTextureForCompletedAct(int completedAct)
    {
        Texture2D[] pool = completedAct switch
        {
            1 => act1ToAct2,
            2 => act2ToAct3,
            _ => Array.Empty<Texture2D>()
        };

        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        int validCount = 0;
        for (int index = 0; index < pool.Length; index++)
        {
            if (pool[index] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int chosenIndex = UnityEngine.Random.Range(0, validCount);
        for (int index = 0; index < pool.Length; index++)
        {
            Texture2D texture = pool[index];
            if (texture == null)
            {
                continue;
            }

            if (chosenIndex == 0)
            {
                return texture;
            }

            chosenIndex--;
        }

        return null;
    }
}
