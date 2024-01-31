<template>
  <div class="login flex items-center justify-center">
    <login-box class="shadow-lg" @success="onLoginSuccess" @failure="onLoginFail" />
  </div>
</template>
<script lang="ts" setup>
import LoginBox from './LoginBox.vue';
import { useRouter } from 'vue-router';
// import http from '@/store/http';
const router = useRouter();

function onLoginSuccess() {
  router.push('/workplace');
}
import { message } from 'ant-design-vue';

function onLoginFail(status, res) {
  console.log(res);
  if (res.code === 0) {
  } else {
    message.error(res.erro, 5);
  }
}
</script>
<style scoped lang="less">
.login {
  height: 100vh;
  // margin-top: -33px;
}
</style>
