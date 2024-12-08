<template>
  <aside class="region__toolbar">
      <menu role="toolbar" class="toolbar__element">
          <span class="element__self">
              <template v-if="isAdminMode">
                  <button @click="setUser">Switch User</button>
                  <button @click="resetGrid">Reset Grid</button>
                  <button @click="revealAll">Reveal All</button>
              </template>

              <div class="element__controls">
                  <p>Admin Mode</p>
                  <label class="controls__switch">
                      <input type="checkbox" v-model="isAdminMode">
                      <span class="slider round"></span>
                  </label>

                  <p>Demo Mode</p>
                  <label class="controls__switch">
                      <input
                          type="checkbox"
                          v-model="connectionStore.demoMode"
                          @change="handleDemoToggle"
                      />
                      <span class="slider round"></span>
                  </label>
                  
                  <p>
                    Realtime:
                    <span
                      :class="{
                        'controls__realtime-status--pending': connectionStore.realtimeStatus === 'Pending',
                        'controls__realtime-status--connected': connectionStore.realtimeStatus === 'Active',
                        'controls__realtime-status--idle': connectionStore.realtimeStatus === 'Idle',
                      }"
                    >
                      {{ connectionStore.realtimeStatus }}
                    </span>
                  </p>
              </div>
          </span>
      </menu>
  </aside>
</template>

<script lang="ts" setup>
import { ref, onMounted } from 'vue';
import { useConnectionStore } from '~/stores/connectionStore';
import { useUserStore } from '~/stores/userStore';
import { useGridStore } from '~/stores/gridStore';

const connectionStore = useConnectionStore();
const userStore = useUserStore();
const gridStore = useGridStore();
const isAdminMode = ref(false);

const revealAll = () => {
  for (let row = 0; row < gridStore.gridSize; row++) {
      for (let col = 0; col < gridStore.gridSize; col++) {
          gridStore.revealCell(row, col);
      }
  }
};

const setUser = () => {
  userStore.setLoggedInUser();
};

const resetGrid = () => {
  gridStore.resetGrid();
  
  userStore.resetScratchCounts();

  if (!connectionStore.demoMode) {
      connectionStore.connection?.invoke("ResetGrid");
  }
};

const handleDemoToggle = async (event:Event) => {
  const target = event.target as HTMLInputElement;
  if (target.checked){
    connectionStore.setRealtimeStatus('Idle');
    return;
  } 
  try {
    connectionStore.setRealtimeStatus('Pending');
    console.log('Reconnecting SignalR...');
  } catch (error) {
    console.error('Error during SignalR reconnection:', error);
    connectionStore.setRealtimeStatus('Idle');
  }
  
};

onMounted(() => {
  connectionStore.attemptSignalRConnection();
  if (connectionStore.connection){
    connectionStore.connection?.on('GridReset', (data: any) => {
    gridStore.resetGrid();

    // Decode the received data and update the grid store with it
    const gridDataArray = Uint8Array.from(atob(data.GridData), (c) => c.charCodeAt(0));
    const revealedDataArray = Uint8Array.from(atob(data.RevealedData), (c) => c.charCodeAt(0));
    gridStore.updateGridData(gridDataArray);
    gridStore.updateRevealedData(revealedDataArray);
  });
  }
  

  connectionStore.connection?.on('NoScratchesLeft', () => {
    console.log('No scratches remaining');
  });

  connectionStore.connection?.on("GridData", (data: { GridData: string; RevealedData: string }) => {

    const gridDataArray = Uint8Array.from(atob(data.GridData), (c) => c.charCodeAt(0));
    const revealedDataArray = Uint8Array.from(atob(data.RevealedData), (c) => c.charCodeAt(0));

    // console.log("Decoded GridData:", gridDataArray);
    // console.log("Decoded RevealedData:", revealedDataArray);

    gridStore.updateGridData(gridDataArray);
    gridStore.updateRevealedData(revealedDataArray);

    console.log("Grid and revealed data updated in store.");
  });

  connectionStore.connection?.on("CellRevealed", (row: number, col: number, value: number) => {
    console.log(`Handler for 'CellRevealed' registered.`);
    console.log(`Cell revealed by server: Row=${row}, Col=${col}, Value=${value}`);

    // Update the grid store with the revealed state and value
    const cellState = value.toString(2).padStart(2, '0') as "00" | "01" | "10";
    gridStore.setCellState(row, col, cellState);
    gridStore.revealCell(row, col);
  });

  connectionStore.connection?.on("CellAlreadyRevealed", (row, col) => {
    console.log(`Cell already revealed: Row=${row}, Col=${col}`);
  });

  connectionStore.connection?.start()
    .then(() => console.log(`Connection started! Client ConnectionId: ${connectionStore.connection?.connectionId}`))
    .catch((err) => console.error("Error starting SignalR connection:", err));
});
</script>


<style lang="scss" scoped>
.region__toolbar {
  position: sticky;
  top: 0;
  width: 200px;
  height: 100vh;
  overflow-y: auto;
  background-color: #f5f5f5;
  box-shadow: 2px 0 5px rgba(0, 0, 0, 0.1);

  .toolbar__element {
    // reset menu html intrinsic styles
    margin: 0;
    padding: 0;
    
    // follow viewport height
    height: 100wh;
    
    display: flex;
    flex-direction: column;
    gap: 20px;

    .element__self {
      display: flex;
      height: inherit;
      flex-direction: column;
      padding: 20px 20px 0;

      height: 90vh;

      padding-bottom: 10px;
      gap: 15px;
      justify-content: end;

      h3 {
        margin: 0;
        font-size: 1.2rem;
        font-weight: bold;
        color: #333;
        text-align: center;
      }

      button {
        display: block;
        width: 100%;
        padding: 10px;
        border: none;
        background-color: #3498db;
        color: white;
        border-radius: 4px;
        cursor: pointer;
        text-align: center;

        &:hover {
          background-color: #2980b9;
        }
      }

      /* Adjusted switch container */
      .element__controls {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 10px; /* Space between rows */

        /* Styles for labels */
        p {
          margin: 0;
          font-size: 0.9rem;
          color: #555;
        }

        /* Align the switch to the right */
        .controls__switch {
          justify-self: end;

          position: relative;
          display: inline-block;
          width: 40px;
          height: 20px;

          input {
            opacity: 0;
            width: 0;
            height: 0;
          }

          .slider {
            position: absolute;
            cursor: pointer;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background-color: #ccc;
            transition: 0.4s;
            border-radius: 20px;

            &.round:before {
              position: absolute;
              content: "";
              height: 14px;
              width: 14px;
              left: 3px;
              bottom: 3px;
              background-color: white;
              transition: 0.4s;
              border-radius: 50%;
            }
        }

          input:checked + .slider {
            background-color: navy;
          }

          input:checked + .slider.round:before {
            transform: translateX(20px);
          }
        }

        .controls__realtime-status {
          font-weight: bold; /* Base styling */
          padding: 2px 5px; /* Optional for better readability */
          border-radius: 4px; /* Optional for visual appeal */

          &--pending {
            color: navy;
          }

          &--connected {
            color: green;
          }

          &--idle {
            color: rgb(162, 75, 0);
          }
        }
      }

      /* Realtime Status Paragraph */
      p:last-of-type {
        font-size: 0.9rem;
        color: #555;

        width: max-content;
        text-align: center
      }
    }
  }
}


</style>
