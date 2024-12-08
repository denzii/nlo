<template>
    <Header />
    <section role='region' class="layout__region">
        <Toolbar />
        <main class="layout__main">
            <slot />
        </main>
    </section>
</template>
  
<script lang="ts" setup>
    import Header from '~/components/Header.vue'
    import Toolbar from '~/components/Toolbar.vue'
</script>
  
<style lang="scss" scoped>
    .layout__region {
        display: flex;
        flex: 1; // Ensures it takes the remaining space below the header
    }

    .layout__main {
        display: flex;
        text-align: center;
        
        width: 100%;
        height: 100%;
        
        // padding: 0 20px 20px 20px; 
        
        overflow-y: hidden;
        
    }
</style>
  