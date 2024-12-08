import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';
import * as signalR from '@microsoft/signalr';
import { useGridStore } from './gridStore';
import { useUserStore } from './userStore';

export const useConnectionStore = defineStore('connection', () => {
    const isPending = ref(true); // SignalR connection is in progress
    const isConnected = ref(false); // Tracks connection success
    const demoMode = ref(false); // Demo Mode toggle
    const connection = ref<signalR.HubConnection | null>(null); // SignalR connection instance

    // Computed value for the realtime status
    const realtimeStatus = computed(() => {
        if (isPending.value) return 'Pending';
        if (isConnected.value) return 'Active';
        return 'Idle';
    });

    // Method to update connection status
    const setRealtimeStatus = (newStatus: string) => {
        if(newStatus === 'Pending'){
            isConnected.value = false;
            isPending.value = true;
        }
        if (newStatus === 'Active') {
            isConnected.value = true;
            isPending.value = false;
        }
        if (newStatus === 'Idle') {
            isConnected.value = false;
            isPending.value = false;
        }
    };

    // Attempt to establish a SignalR connection
    const attemptSignalRConnection = async () => {
        const gridStore = useGridStore();
        const userStore = useUserStore();
        const connectionStore = useConnectionStore();

        isPending.value = true;
        try {
          connection.value = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5218/gridhub", { transport: signalR.HttpTransportType.WebSockets })
            .configureLogging(signalR.LogLevel.Debug)
            .withAutomaticReconnect()
            .build();
            connection.value.on("UserAssigned", (userId: number) => {
                if (!connectionStore.demoMode) {
                    userStore.setLoggedInUser(userId); // Ensure backend mode syncs correctly
                }
            });
    
            connection.value.on("RejectConnection", (message: string) => {
                console.error(`Connection rejected: ${message}`);
                alert(message);
            });
          // Attach event handlers
          connection.value.on('GridData', (data: { GridData: string; RevealedData: string }) => {
            console.log('Received GridData:', data);
            const gridDataArray = Uint8Array.from(atob(data.GridData), c => c.charCodeAt(0));
            const revealedDataArray = Uint8Array.from(atob(data.RevealedData), c => c.charCodeAt(0));
            gridStore.updateGridData(gridDataArray);
            gridStore.updateRevealedData(revealedDataArray);
          });
      
          connection.value.on('CellRevealed', (row, col, value) => {
            console.log(`Cell revealed: Row=${row}, Col=${col}, Value=${value}`);
            gridStore.setCellState(row, col, value.toString(2).padStart(2, '0') as "00" | "01" | "10");
            gridStore.revealCell(row, col);
          });
      
          await connection.value.start();
          console.log('SignalR connection established.');
          isConnected.value = true;
        } catch (error) {
          console.error('SignalR connection failed:', error);
          isConnected.value = false;
        } finally {
          isPending.value = false;
        }
      };
      
    // Stop the SignalR connection
    const stopConnection = async () => {
        if (connection.value) {
            console.log('Stopping SignalR connection...');
            try {
                await connection.value.stop();
                console.log('SignalR connection stopped.');
            } catch (error) {
                console.error('Error stopping SignalR connection:', error);
            } finally {
                connection.value = null; // Reset the connection instance
                isConnected.value = false; // Ensure connection status reflects this
            }
        }
    };

    // Toggle Demo Mode and manage connection state
    const toggleDemoMode = async () => {
        if (demoMode.value) {
            // Switching to Demo Mode
            await stopConnection(); // Ensure SignalR connection is dropped
            setRealtimeStatus('Idle');
            console.log('Switched to Demo Mode: SignalR connection stopped.');
        } else {
            // Switching to Real-time Mode
            setRealtimeStatus('Pending');
            console.log('Switching to Real-time Mode...');
            await attemptSignalRConnection(); // Attempt to reconnect
            if (isConnected.value) {
                setRealtimeStatus('Active');
                console.log('Switched to Real-time Mode: SignalR connection established.');
            } else {
                setRealtimeStatus('Idle');
                console.warn('Failed to connect to SignalR. Keeping Real-time Mode in Idle state.');
            }
        }
    };

    return {
        isPending,
        isConnected,
        demoMode,
        connection,
        realtimeStatus,
        setRealtimeStatus,
        attemptSignalRConnection,
        stopConnection,
        toggleDemoMode
    };
});
