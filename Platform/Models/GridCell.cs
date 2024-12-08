namespace Platform.Models
{
    public class GridCell
    {
        public int Id { get; set; }
        public byte[] GridData { get; set; } = new byte[2500];
        public byte[] RevealedData { get; set; } = new byte[2500]; 

    public static byte[] GenerateRandom()
    {
        var gridData = new byte[2500]; // 10,000 cells represented in 2 bits per cell
        var random = new Random();

        // Generate a shuffled list of all cell indices
        var indices = Enumerable.Range(0, 10000).OrderBy(_ => random.Next()).ToArray();

        // Assign the grand prize (01)
        PlacePrize(gridData, indices[0], 0b01);

        // Assign 100 consolation prizes (10)
        for (int i = 1; i <= 100; i++)
        {
            // Place a consolation prize at each index in the shuffled list by setting the 2-bit value to 10
            // (0b10 in binary representation)
            PlacePrize(gridData, indices[i], 0b10);
        }

        return gridData;
    }

    /// <summary>
    /// Places a prize at the specified cell index in the grid data.
    /// </summary>
    /// <param name="gridData">The grid data byte array.</param>
    /// <param name="cellIndex">The index of the cell to update.</param>
    /// <param name="prizeValue">The value of the prize (in 2-bit representation).</param>
    private static void PlacePrize(byte[] gridData, int cellIndex, byte prizeValue)
    {
        // Calculate the byte index and bit offset for the cell index
        // Each cell is represented by 2 bits in the byte array
        // The prize value is stored in these 2 bits
        // The byte index is the cell index divided by 4 (since each byte holds 4 cells)
        // The bit offset is the remainder of the cell index divided by 4 multiplied by 2 (since each cell is 2 bits)
        // This allows us to access the correct byte and bits for the cell index
        int byteIndex = cellIndex / 4; // Each byte holds 4 cells
        int bitOffset = (cellIndex % 4) * 2; // Each cell is 2 bits

        // Clear the cell before setting the prize value
        // This is done by masking the 2 bits for the cell with 0 (00) using the bitwise AND operator
        gridData[byteIndex] &= (byte)~(0b11 << bitOffset);

        // Set the prize value
        // This is done by setting the 2 bits for the cell to the prize value using the bitwise OR operator
        gridData[byteIndex] |= (byte)(prizeValue << bitOffset);
    }
    }

  
}
