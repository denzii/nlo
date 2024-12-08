import { defineStore } from 'pinia';
import { useConnectionStore } from './connectionStore';
import { useUserStore } from './userStore';
export const useGridStore = defineStore('gridStore', {
  state: () => ({
    grid: new Uint8Array(2500), // Each Uint8 holds 4 cells (2 bits per cell)
    revealed: new Uint8Array(2500), // Each Uint8 holds 8 cells (1 bit per cell for revealed state)
    gridSize: 100, // Assume a 100x100 grid
    visibleRows: [] as number[], // Visible rows
    visibleCols: [] as number[], // Visible columns
  }),

  getters: {
    getCellState: (state) => (row: number, col: number) => {
      const index = row * state.gridSize + col;
      const byteIndex = Math.floor(index / 4);
      const bitOffset = (index % 4) * 2;
      const mask = 0b11 << bitOffset;
      const cellState = (state.grid[byteIndex] & mask) >> bitOffset;
      return cellState.toString(2).padStart(2, '0');
    },
    isCellRevealed: (state) => (row: number, col: number) => {
      const index = row * state.gridSize + col;
      const byteIndex = Math.floor(index / 8);
      const bitOffset = index % 8;
      const mask = 1 << bitOffset;
      return Boolean(state.revealed[byteIndex] & mask); 
  },
  },

  actions: {
    setCellState(row: number, col: number, state: "00" | "01" | "10") {
      const index = row * this.gridSize + col;
      const byteIndex = Math.floor(index / 4);
      const bitOffset = (index % 4) * 2;
    
      const stateValue = parseInt(state, 2) << bitOffset;
      const mask = ~(0b11 << bitOffset);
    
      const updatedGrid = new Uint8Array(this.grid); // Clone the grid
      updatedGrid[byteIndex] = (updatedGrid[byteIndex] & mask) | stateValue;
    
      this.grid = updatedGrid; // Replace the grid
    },
    
    
    revealCell(row: number, col: number) {
      const index = row * this.gridSize + col;
      const byteIndex = Math.floor(index / 8);
      const bitOffset = index % 8;
      const mask = 1 << bitOffset;
    
      const updatedRevealed = new Uint8Array(this.revealed); // Clone the revealed array
      updatedRevealed[byteIndex] |= mask;
    
      this.revealed = updatedRevealed; // Replace the revealed array
    },
    updateVisibleRanges(scrollTop: number, scrollLeft: number, clientHeight: number, clientWidth: number) {
      
      const rowHeight = 40; // Define cell height
      const cellWidth = 70; // Define cell width

      const startRow = Math.max(0, Math.floor(scrollTop / rowHeight) - 2);
      const endRow = Math.min(this.gridSize - 1, Math.ceil((scrollTop + clientHeight) / rowHeight) + 2);

      const startCol = Math.max(0, Math.floor(scrollLeft / cellWidth) - 2);
      const endCol = Math.min(this.gridSize - 1, Math.ceil((scrollLeft + clientWidth) / cellWidth) + 2);

      this.visibleRows = Array.from({ length: endRow - startRow + 1 }, (_, i) => startRow + i);
      this.visibleCols = Array.from({ length: endCol - startCol + 1 }, (_, i) => startCol + i);

      ;

      // Trigger backend update
      const connectionStore = useConnectionStore();
      const userStore = useUserStore();
      const currentUserId = userStore.loggedInUserId;

      connectionStore.connection?.invoke('UpdateVisibleCells', currentUserId, this.visibleRows, this.visibleCols)
        .then((response) => console.log(`Visible rows: ${this.visibleRows}, cols: ${this.visibleCols} sent to server., response: ${response}`))
        .catch(err => console.error('Error sending visible cells to server:', err));
    },
        resetGrid() {
            // Create new Uint8Arrays to trigger reactivity explicitly
            const updatedGrid = new Uint8Array(this.grid.length);
            const updatedRevealed = new Uint8Array(this.revealed.length);
            // Reset all cells to '00' and no revealed states
            updatedGrid.fill(0);
            updatedRevealed.fill(0);

            // // Clear all modifiers (force re-render by resetting grid state)
            // const cells = document.querySelectorAll('.grid__cell');
            // cells.forEach((cell) => {
            //   cell.className = 'grid__cell'; // Reset to base class
            // });

            // Reset all cells to '00'
            updatedGrid.fill(0);
            updatedRevealed.fill(0);
          
            // Shuffle indices for prize placement
            const totalCells = this.gridSize * this.gridSize;
            const indices = Array.from({ length: totalCells }, (_, i) => i);
            for (let i = indices.length - 1; i > 0; i--) {
              const j = Math.floor(Math.random() * (i + 1));
              [indices[i], indices[j]] = [indices[j], indices[i]];
            }
          
            // Assign the big prize to the first index
            const bigPrizeIndex = indices[0];
            const bigPrizeRow = Math.floor(bigPrizeIndex / this.gridSize);
            const bigPrizeCol = bigPrizeIndex % this.gridSize;
            const bigPrizeValue = parseInt('01', 2); // Big prize
            const bigPrizeByteIndex = Math.floor(bigPrizeIndex / 4);
            const bigPrizeBitOffset = (bigPrizeIndex % 4) * 2;
            updatedGrid[bigPrizeByteIndex] |= bigPrizeValue << bigPrizeBitOffset;
          
            // Assign consolation prizes to the next 100 indices
            for (let i = 1; i <= 100; i++) {
              const consolationIndex = indices[i];
              const consolationRow = Math.floor(consolationIndex / this.gridSize);
              const consolationCol = consolationIndex % this.gridSize;
              const consolationValue = parseInt('10', 2); // Consolation prize
              const consolationByteIndex = Math.floor(consolationIndex / 4);
              const consolationBitOffset = (consolationIndex % 4) * 2;
              updatedGrid[consolationByteIndex] |= consolationValue << consolationBitOffset;
            }
          
            // Update reactive state
            this.grid = updatedGrid; // Trigger reactivity
            this.revealed = updatedRevealed; // Reset revealed state
          },
          updateGridData(gridData: Uint8Array) {
            if (gridData.length !== this.grid.length) {
              console.error('Received grid data does not match expected size.');
              return;
            }
      
            // Create a new Uint8Array for reactivity
            const updatedGrid = new Uint8Array(gridData);
            this.grid = updatedGrid;
          },
          updateRevealedData(revealedData: Uint8Array) {
            if (revealedData.length !== this.revealed.length) {
              console.error("Received revealed data does not match expected size.");
              return;
            }
          
            // Create a new Uint8Array for reactivity
            const updatedRevealed = new Uint8Array(revealedData);
            this.revealed = updatedRevealed;
          },
          cellAlreadyRevealed(row:number, col:number) {
            console.log(`Cell already revealed: Row=${row}, Col=${col}`);
          }
  },
});
