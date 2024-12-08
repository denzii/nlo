import { defineStore } from 'pinia';
import { useConnectionStore } from './connectionStore';

export const useUserStore = defineStore('userStore', {
  state: () => ({
    loggedInUserId: 1, // Default for demo mode
    scratchCounts: {} as Record<number, number>, // Scratch counts
    demoUsers: [1, 2, 3, 4], // Dummy user IDs for demo mode
  }),
  actions: {
    setLoggedInUser(id?: number) {
      const connectionStore = useConnectionStore();
      if (connectionStore.demoMode) {
          // Demo Mode: Rotate through dummy users
          const currentIndex = this.demoUsers.indexOf(this.loggedInUserId);
          this.loggedInUserId = this.demoUsers[(currentIndex + 1)];
      } else if (id) {
          this.loggedInUserId = id;
      } else{
          connectionStore.connection?.invoke('SwitchUser', this.loggedInUserId);
      }
    },
    resetScratchCounts() {
      this.scratchCounts = {};
    },
    incrementScratchCount(userId: number) {
      if (!this.scratchCounts[userId]) {
        this.scratchCounts[userId] = 0;
      }
      this.scratchCounts[userId]++;
    },
    getScratchCount(userId: number) {
      return this.scratchCounts[userId] || 0;
    },
  },
});
