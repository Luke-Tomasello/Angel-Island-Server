/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* Scripts\misc\DistanceMatrix.cs
 * CHANGELOG:
 *  9/10/22, Yoar
 *      Renamed to DistanceMatrix.
 *	1/14/22, Yoar
 *		Initial version. Class for maths functions.
 */

using System;

namespace Server
{
    public static class DistanceMatrix
    {
        /// <summary>
        /// Calculates, for each position in the matrix, the Chebyshev distance to the nearest zero.
        /// Based on <see href="https://blog.ostermiller.org/efficiently-implementing-dilate-and-erode-image-functions/"/>.
        /// </summary>
        public static void Fill(ushort[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (matrix[i, j] != 0)
                    {
                        matrix[i, j] = (ushort)Math.Max(rows, cols);

                        if (i != 0)
                            matrix[i, j] = (ushort)Math.Min(matrix[i, j], matrix[i - 1, j] + 1);

                        if (j != 0)
                            matrix[i, j] = (ushort)Math.Min(matrix[i, j], matrix[i, j - 1] + 1);

                        if (i != 0 && j != 0)
                            matrix[i, j] = (ushort)Math.Min(matrix[i, j], matrix[i - 1, j - 1] + 1);

                        if (i != 0 && j + 1 != cols)
                            matrix[i, j] = (ushort)Math.Min(matrix[i, j], matrix[i - 1, j + 1] + 1);
                    }
                }
            }

            for (int i = rows - 1; i >= 0; i--)
            {
                for (int j = cols - 1; j >= 0; j--)
                {
                    if (i + 1 != rows)
                        matrix[i, j] = (ushort)Math.Min(matrix[i, j], matrix[i + 1, j] + 1);

                    if (j + 1 != cols)
                        matrix[i, j] = (ushort)Math.Min(matrix[i, j], matrix[i, j + 1] + 1);

                    if (i + 1 != rows && j + 1 != cols)
                        matrix[i, j] = (ushort)Math.Min(matrix[i, j], matrix[i + 1, j + 1] + 1);

                    if (i + 1 != rows && j != 0)
                        matrix[i, j] = (ushort)Math.Min(matrix[i, j], matrix[i + 1, j - 1] + 1);
                }
            }
        }
    }
}