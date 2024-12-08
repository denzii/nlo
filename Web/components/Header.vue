<template>
    <header class="layout__header">
        <nav class="header__nav">
            <ul class="nav__menu">
                <li class="menu__element">
                    <span class="element__logo">
                        <h4>NLO</h4>
                    </span>
                    <span class="element__user">
                        <img :src="loggedInUserAvatar.src" :alt="loggedInUserAvatar.alt" class="user-avatar" />
                    </span>
                </li>
            </ul>
        </nav>
    </header>
</template>
  
<script lang="ts" setup>
    import { computed } from 'vue';
    import { useUserStore } from '../stores/userStore';
    import user1 from '@/assets/images/user1.png';
    import user2 from '@/assets/images/user2.png';
    import user3 from '@/assets/images/user3.png';
    import user4 from '@/assets/images/user4.png';

    const avatars = [
        { id: 1, src: user1, alt: 'Deniz' },
        { id: 2, src: user2, alt: 'Kim' },
        { id: 3, src: user3, alt: 'Pinpon' },
        { id: 4, src: user4, alt: 'Ash' }
    ];
    const userStore = useUserStore();

    const loggedInUserAvatar = computed(() => {
        const userId = userStore.loggedInUserId; 
    
        return avatars.find((avatar) => avatar.id === userId) || avatars[0];
    }
);
</script>
  
<style lang="scss" scoped>
.layout__header {
    display: flex; 
    align-items: center;
    color: white;
    height: 60px;

    .header__nav {
        padding: 10px 20px;
        flex: 1;
        justify-content: space-between;
        background-color: #3498db;

        .nav__menu {
            display: flex;
            align-items: center;
            list-style: none;
            margin: 0;
            padding: 0;
            gap: 10px;

            .menu__element {
                display: flex;
                align-items: center;
                margin-right: 20px;
                width:95vw;
                justify-content: space-between;

                .element__logo {
                    h4 {
                        margin: 0;
                        font-size: 18px;
                    }
                }

                .element__user {
                    display: flex;
                    align-items: center;
                    gap: 10px;

                    .user-avatar {
                        width: 40px;
                        height: 40px;
                        border-radius: 50%;
                        border: 2px solid white;
                    }

                    p {
                        margin: 0;
                        color: white;
                        font-size: 14px;
                    }
                }
            }
        }
    }
}
</style>
  