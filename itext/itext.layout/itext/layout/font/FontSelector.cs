/*
This file is part of the iText (R) project.
Copyright (c) 1998-2018 iText Group NV
Authors: Bruno Lowagie, Paulo Soares, et al.

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License version 3
as published by the Free Software Foundation with the addition of the
following permission added to Section 15 as permitted in Section 7(a):
FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
ITEXT GROUP. ITEXT GROUP DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
OF THIRD PARTY RIGHTS

This program is distributed in the hope that it will be useful, but
WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
or FITNESS FOR A PARTICULAR PURPOSE.
See the GNU Affero General Public License for more details.
You should have received a copy of the GNU Affero General Public License
along with this program; if not, see http://www.gnu.org/licenses or write to
the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
Boston, MA, 02110-1301 USA, or download the license from the following URL:
http://itextpdf.com/terms-of-use/

The interactive user interfaces in modified source and object code versions
of this program must display Appropriate Legal Notices, as required under
Section 5 of the GNU Affero General Public License.

In accordance with Section 7(b) of the GNU Affero General Public License,
a covered work must retain the producer line in every PDF that is created
or manipulated using iText.

You can be released from the requirements of the license by purchasing
a commercial license. Buying such a license is mandatory as soon as you
develop commercial activities involving the iText software without
disclosing the source code of your own applications.
These activities include: offering paid services to customers as an ASP,
serving PDFs on the fly in a web application, shipping iText with a closed
source product.

For more information, please contact iText Software Corp. at this
address: sales@itextpdf.com
*/
using System;
using System.Collections.Generic;
using iText.IO.Util;

namespace iText.Layout.Font {
    /// <summary>Sort given set of fonts according to font name and style.</summary>
    public class FontSelector {
        protected internal IList<FontInfo> fonts;

        private const int EXPECTED_FONT_IS_BOLD_AWARD = 5;

        private const int EXPECTED_FONT_IS_NOT_BOLD_AWARD = 3;

        private const int EXPECTED_FONT_IS_ITALIC_AWARD = 5;

        private const int EXPECTED_FONT_IS_NOT_ITALIC_AWARD = 3;

        private const int EXPECTED_FONT_IS_MONOSPACED_AWARD = 5;

        private const int EXPECTED_FONT_IS_NOT_MONOSPACED_AWARD = 1;

        private const int FONT_FAMILY_EQUALS_AWARD = 13;

        /// <summary>Create new FontSelector instance.</summary>
        /// <param name="allFonts">Unsorted set of all available fonts.</param>
        /// <param name="fontFamilies">Sorted list of preferred font families.</param>
        public FontSelector(ICollection<FontInfo> allFonts, IList<String> fontFamilies, FontCharacteristics fc) {
            this.fonts = new List<FontInfo>(allFonts);
            //Possible issue in .NET, virtual protected member in constructor.
            JavaCollectionsUtil.Sort(this.fonts, GetComparator(fontFamilies, fc));
        }

        /// <summary>The best font match.</summary>
        /// <remarks>
        /// The best font match.
        /// If any font from
        /// <see cref="GetFonts()"/>
        /// doesn't contain requested glyphs, this font will be used.
        /// </remarks>
        /// <returns>the best matched font</returns>
        public FontInfo BestMatch() {
            return fonts[0];
        }

        // fonts is sorted best to worst, get(0) returns the best matched FontInfo
        /// <summary>Sorted set of fonts.</summary>
        /// <returns>sorted set of fonts</returns>
        public IEnumerable<FontInfo> GetFonts() {
            return fonts;
        }

        protected internal virtual IComparer<FontInfo> GetComparator(IList<String> fontFamilies, FontCharacteristics
             fc) {
            return new FontSelector.PdfFontComparator(fontFamilies, fc);
        }

        private class PdfFontComparator : IComparer<FontInfo> {
            internal IList<String> fontFamilies;

            internal IList<FontCharacteristics> fontStyles;

            internal PdfFontComparator(IList<String> fontFamilies, FontCharacteristics fc) {
                this.fontFamilies = new List<String>();
                this.fontStyles = new List<FontCharacteristics>();
                if (fontFamilies != null && fontFamilies.Count > 0) {
                    foreach (String fontFamily in fontFamilies) {
                        String lowercaseFontFamily = fontFamily.ToLowerInvariant();
                        this.fontFamilies.Add(lowercaseFontFamily);
                        this.fontStyles.Add(ParseFontStyle(lowercaseFontFamily, fc));
                    }
                }
                else {
                    this.fontStyles.Add(fc);
                }
            }

            public virtual int Compare(FontInfo o1, FontInfo o2) {
                int res = 0;
                for (int i = 0; i < fontFamilies.Count && res == 0; i++) {
                    FontCharacteristics fc = fontStyles[i];
                    String fontFamily = fontFamilies[i];
                    if (fontFamily.EqualsIgnoreCase("monospace")) {
                        fc.SetMonospaceFlag(true);
                    }
                    bool isLastFontFamilyToBeProcessed = i == fontFamilies.Count - 1;
                    res = CharacteristicsSimilarity(fontFamily, fc, o2, isLastFontFamilyToBeProcessed) - CharacteristicsSimilarity
                        (fontFamily, fc, o1, isLastFontFamilyToBeProcessed);
                }
                return res;
            }

            private static FontCharacteristics ParseFontStyle(String fontFamily, FontCharacteristics fc) {
                if (fc == null) {
                    fc = new FontCharacteristics();
                }
                if (fc.IsUndefined()) {
                    if (fontFamily.Contains("bold")) {
                        fc.SetBoldFlag(true);
                    }
                    if (fontFamily.Contains("italic") || fontFamily.Contains("oblique")) {
                        fc.SetItalicFlag(true);
                    }
                }
                return fc;
            }

            /// <summary>
            /// // TODO DEVSIX-2050 Update the documentation once the changes are accepted
            /// This method is used to compare two fonts (the first is described by fontInfo,
            /// the second is described by fc and fontFamily) and measure their similarity.
            /// </summary>
            /// <remarks>
            /// // TODO DEVSIX-2050 Update the documentation once the changes are accepted
            /// This method is used to compare two fonts (the first is described by fontInfo,
            /// the second is described by fc and fontFamily) and measure their similarity.
            /// The more the fonts are similar the higher the score is.
            /// <p>
            /// We check whether the fonts are both:
            /// a) bold
            /// b) italic
            /// c) monospaced
            /// <p>
            /// We also check whether the font names are identical. There are two blocks of conditions:
            /// "equals" and "contains". They cannot be satisfied simultaneously.
            /// Some remarks about these checks:
            /// a) "contains" block checks are much easier to be satisfied so one can get award from this block
            /// higher than from "equals" block only if all "contains" conditions are satisfied.
            /// b) since ideally all conditions of a certain block are satisfied simultaneously, it may result
            /// in highly inflated score. So we decrease an award for other conditions of the block
            /// if one has been already satisfied.
            /// </remarks>
            private static int CharacteristicsSimilarity(String fontFamily, FontCharacteristics fc, FontInfo fontInfo, 
                bool isLastFontFamilyToBeProcessed) {
                bool isFontBold = fontInfo.GetDescriptor().IsBold() || fontInfo.GetDescriptor().GetFontWeight() > 500;
                bool isFontItalic = fontInfo.GetDescriptor().IsItalic() || fontInfo.GetDescriptor().GetItalicAngle() < 0;
                bool isFontMonospace = fontInfo.GetDescriptor().IsMonospace();
                int score = 0;
                // if font-family is monospace, serif or sans-serif, actual font's name shouldn't be checked
                bool fontFamilySetByCharacteristics = false;
                // check whether we want to select a monospace, TODO DEVSIX-1034 serif or sans-serif font
                if (fc.IsMonospace()) {
                    fontFamilySetByCharacteristics = true;
                    if (isFontMonospace) {
                        score += EXPECTED_FONT_IS_MONOSPACED_AWARD;
                    }
                    else {
                        score -= EXPECTED_FONT_IS_MONOSPACED_AWARD;
                    }
                }
                else {
                    if (isFontMonospace) {
                        score -= EXPECTED_FONT_IS_NOT_MONOSPACED_AWARD;
                    }
                }
                if (!fontFamilySetByCharacteristics) {
                    // if alias is set, fontInfo's descriptor should not be checked
                    if (!"".Equals(fontFamily) && (null == fontInfo.GetAlias() && fontInfo.GetDescriptor().GetFamilyNameLowerCase
                        ().Equals(fontFamily) || (null != fontInfo.GetAlias() && fontInfo.GetAlias().ToLowerInvariant().Equals
                        (fontFamily)))) {
                        score += FONT_FAMILY_EQUALS_AWARD;
                    }
                    else {
                        if (!isLastFontFamilyToBeProcessed) {
                            return score;
                        }
                    }
                }
                // calculate style characteristics
                if (fc.IsBold()) {
                    if (isFontBold) {
                        score += EXPECTED_FONT_IS_BOLD_AWARD;
                    }
                    else {
                        score -= EXPECTED_FONT_IS_BOLD_AWARD;
                    }
                }
                else {
                    if (isFontBold) {
                        score -= EXPECTED_FONT_IS_NOT_BOLD_AWARD;
                    }
                }
                if (fc.IsItalic()) {
                    if (isFontItalic) {
                        score += EXPECTED_FONT_IS_ITALIC_AWARD;
                    }
                    else {
                        score -= EXPECTED_FONT_IS_ITALIC_AWARD;
                    }
                }
                else {
                    if (isFontItalic) {
                        score -= EXPECTED_FONT_IS_NOT_ITALIC_AWARD;
                    }
                }
                return score;
            }
        }
    }
}
