using System;

namespace KKDailyOutfits
{
    // No whole-coordinate load/copy: accessories, makeup and other clothes stay intact.
    internal static class UnderwearCopy
    {
        internal const int Bra = (int)ChaFileDefine.ClothesKind.bra;
        internal const int Shorts = (int)ChaFileDefine.ClothesKind.shorts;

        internal static void Apply(ChaFileClothes source, ChaFileClothes target)
        {
            if (source == null || target == null) throw new ArgumentNullException();
            // Finish both deep copies before mutating the destination.
            var bra = Clone(source.parts[Bra]);
            var shorts = Clone(source.parts[Shorts]);
            target.parts[Bra] = bra;
            target.parts[Shorts] = shorts;
            // hideBraOpt/hideShortsOpt belong to the outer garment's masking rules.
            // Preserve them, along with subPartsId and every other clothing slot.
        }

        private static ChaFileClothes.PartsInfo Clone(ChaFileClothes.PartsInfo part)
        {
            if (part == null) throw new ArgumentException("Missing underwear part.");
            var copy = new ChaFileClothes.PartsInfo
            {
                id = part.id,
                emblemeId = part.emblemeId,
                emblemeId2 = part.emblemeId2,
                sleevesType = part.sleevesType,
                hideOpt = part.hideOpt == null ? null : (bool[])part.hideOpt.Clone()
            };
            if (part.colorInfo == null) copy.colorInfo = null;
            else
            {
                copy.colorInfo = new ChaFileClothes.PartsInfo.ColorInfo[part.colorInfo.Length];
                for (int i = 0; i < part.colorInfo.Length; i++)
                {
                    var color = part.colorInfo[i];
                    if (color == null) throw new ArgumentException("Missing underwear color.");
                    copy.colorInfo[i] = new ChaFileClothes.PartsInfo.ColorInfo
                    {
                        baseColor = color.baseColor, pattern = color.pattern,
                        tiling = color.tiling, patternColor = color.patternColor
                    };
                }
            }
            return copy;
        }
    }
}
