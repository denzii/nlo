<template>
  <section
    role="grid"
    class="main__grid"
    :class="{ 'main__grid--blurred': connectionStore.realtimeStatus === 'Pending' }"
    ref="mainSection"
    @scroll="handleScrollThrottled"
    aria-label="Game Map"
  >
    <div
      v-for="row in visibleRows"
      :key="row"
      class="grid__row"
      :style="{ '--row': row }"
      role="row"
      :aria-label="'Row ' + row"
    >
    <span
      v-for="col in visibleCols"
      :key="col"
      class="grid__cell"
      :style="{ '--col': col }"
      :aria-label="'Cell ' + row + ', ' + col"
      :class="{
        'grid__cell--scratchable': isScratchable(row, col),
        'grid__cell--scratched': gridStore.isCellRevealed(row, col),
        'grid__cell--chicken-dinner': gridStore.getCellState(row, col) === '01' && gridStore.isCellRevealed(row, col),
        'grid__cell--not-a-cigar': gridStore.getCellState(row, col) === '10' && gridStore.isCellRevealed(row, col),
        'grid__cell--sorry': gridStore.getCellState(row, col) === '00' && gridStore.isCellRevealed(row, col),
      }"
      role="gridcell"
      @click="handleCellClick(row, col)"
    >
  {{ getDisplayState(row, col) }}
</span>

    </div>
  </section>
</template>

  
<script lang="ts" setup>
  import { ref, onMounted, watch } from 'vue';
  import { throttle } from 'lodash';
  import { useConnectionStore } from '~/stores/connectionStore';
  import { useGridStore } from '~/stores/gridStore';
  import { useUserStore } from '~/stores/userStore'

  const connectionStore = useConnectionStore();
  const gridStore = useGridStore();
  const userStore = useUserStore();

  const rowHeight = 40;
  const cellWidth = 70;
  const totalRows = 100;
  const totalCols = 100;
  
  const mainSection = ref<HTMLElement | null>(null);
  const visibleRows = ref<number[]>([]);
  const visibleCols = ref<number[]>([]);
  
  const isScratchable = (row: number, col: number): boolean => (
    connectionStore.demoMode && !gridStore.isCellRevealed(row, col)
  );
  
  const getDisplayState = (row: number, col: number): string => {
    if (!connectionStore.demoMode || !gridStore.isCellRevealed(row, col))
      return "";

    const cellState = gridStore.getCellState(row, col);

    if (cellState === '01') return 'Big Prize';
    if (cellState === '10') return 'Consolation Prize';
    return 'No Prize';
  };
  
  const handleCellClick = async (row: number, col: number): Promise<void> => {
    const currentUserId = userStore.loggedInUserId;

    if (connectionStore.demoMode) {
      if (userStore.getScratchCount(currentUserId) >= 5) {
        console.log(`User ${currentUserId} has reached the maximum number of scratches.`);
        return;
      }

      if (!isScratchable(row, col)) return;

      gridStore.revealCell(row, col);
      userStore.incrementScratchCount(currentUserId);
      console.log(`Revealed cell at row=${row}, col=${col} by user ${currentUserId}`);
    } else {
      try {
        console.log('Invoking CellClicked on server...');
        await connectionStore.connection?.invoke('CellClicked', currentUserId, row, col);
        console.log(`Cell click sent to server: row=${row}, col=${col}`);
      } catch (error) {
        console.error('Error sending cell click to server:', error);
      }
    }
  };

const updateVisibleRanges = (
  scrollTop: number,
  scrollLeft: number,
  clientHeight: number,
  clientWidth: number
): void => {
  const startRow = Math.max(0, Math.floor(scrollTop / rowHeight) - 2);
  const endRow = Math.min(totalRows - 1, Math.ceil((scrollTop + clientHeight) / rowHeight) + 2);

  const startCol = Math.max(0, Math.floor(scrollLeft / cellWidth) - 2);
  const endCol = Math.min(totalCols - 1, Math.ceil((scrollLeft + clientWidth) / cellWidth) + 2);

  visibleRows.value = Array.from({ length: endRow - startRow + 1 }, (_, i) => startRow + i);
  visibleCols.value = Array.from({ length: endCol - startCol + 1 }, (_, i) => startCol + i);

  const currentUserId = userStore.loggedInUserId;
  connectionStore.connection?.invoke('UpdateVisibleCells', currentUserId, visibleRows.value, visibleCols.value)
    .then(() => console.log(`Visible cells sent to server as rows: ${visibleRows.value}, cols: ${visibleCols.value}`))
    .catch(err => console.error('Error sending visible cells to server:', err));
};
  
  // Throttle scroll handling
  const handleScrollThrottled = throttle(() => {
    if (!mainSection.value) return;
    const { scrollTop, scrollLeft, clientHeight, clientWidth } = mainSection.value;
    updateVisibleRanges(scrollTop, scrollLeft, clientHeight, clientWidth);
  }, 350);

  watch(() => connectionStore.realtimeStatus, async (newStatus) => {
  if (newStatus === 'Active') {
    console.log('Connection established, sending visible cells.');
    if (mainSection.value) {
      const { scrollTop, scrollLeft, clientHeight, clientWidth } = mainSection.value;
      updateVisibleRanges(scrollTop, scrollLeft, clientHeight, clientWidth);
    }
  }
  else if (newStatus == 'Idle') {
    await connectionStore.connection?.stop();
    gridStore.resetGrid();
    userStore.resetScratchCounts();
    connectionStore.connection = null;
    connectionStore.demoMode = true;
    console.log('Demo Mode is now active.');
  }
  else if (newStatus == 'Pending') {
    await connectionStore.attemptSignalRConnection();
      if (connectionStore.isConnected) {
        connectionStore.setRealtimeStatus('Active');
        console.log('Real-time Mode is now active.');
        return
      } 
      console.warn('SignalR reconnection failed. Keeping Demo Mode ON');
      connectionStore.setRealtimeStatus('Idle');
  }
});
// watch(
//       () => userStore.loggedInUserId,
//       (newUserId) => {
//           console.log(`User switched to ID: ${newUserId}`);
//       }
//   );
//   watch([visibleRows, visibleCols], ([rows, cols]) => {
//   rows.forEach((row) => {
//     cols.forEach((col) => {
//       const cellState = gridStore.getCellState(row, col);
//       const isRevealed = gridStore.isCellRevealed(row, col);

//       console.log(`Row: ${row}, Col: ${col}, State: ${cellState}, Revealed: ${isRevealed}`);
//     });
//   });
// });


//   watch(
//     () => gridStore.grid,
//     (newGrid) => {
//       console.log("Grid updated:", newGrid);
//     },
//     { deep: true }
//   );

//   watch(
//     () => gridStore.revealed,
//     (newRevealed) => {
//       console.log("Revealed updated:", newRevealed);
//     },
//     { deep: true }
//   );

  onMounted(() => {
    gridStore.resetGrid();

    if (mainSection.value) {
      const { scrollTop, scrollLeft, clientHeight, clientWidth } = mainSection.value;
      updateVisibleRanges(scrollTop, scrollLeft, clientHeight, clientWidth);
    }
  });
</script>
  
  
<style lang="scss" scoped>
  .main__grid {
    width: 100%;
    height: 91vh;
    overflow-y: auto;
    overflow-x: auto;
    border: 1px solid #ddd;
    position: relative;
  
    &--blurred {
      filter: blur(5px);
      pointer-events: none;
      user-select: none;
    }
  
    .grid__row {
      display: flex;
      position: absolute;
      height: 40px;
      width: 100%;
      top: calc(var(--row) * 40px);
    }
  
    .grid__cell {
      position: absolute;
      height: 40px;
      background-color: #3498db;
      border: 1px solid #fff;
      box-sizing: border-box;
      text-align: center;
      line-height: 40px;
      transition: background-color 0.2s ease;
      width: 70px;
      left: calc(var(--col) * 70px);

      &:hover {
        background-color: navy;
      }
      &--scratchable {
        cursor: pointer;
      }

      &--scratched {
        background-color: #757575;
        color: white;
        cursor: not-allowed;
      }
      &--chicken-dinner{
        background-color: green;
      }
      &--not-a-cigar{
        background-color: orange;
      }
      &--sorry{
        background-color: gray;
      }
    }
  }
</style>
  