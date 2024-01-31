<template>
  <a-card :bordered="false" :bodyStyle='{}'>
    <a-form :model="formState" :label-col="labelCol" :rules="rules" :wrapper-col="wrapperCol" ref="formRef">

      <a-form-item label="DNS服务商" ref="domainServer" name="domainServer">
        <a-radio-group v-model:value="formState.domainServer" button-style="solid" @change="oncouldChange">
          <a-radio-button :value="item.key" :disabled='componentDisabled' :key="index" v-for="(item,index) in cloudNames">{{item.value}}</a-radio-button>
        </a-radio-group>
      </a-form-item>

      <a-form-item has-feedback label="主域名" ref="domain" name="domain">
        <a-input v-model:value="formState.domain" :disabled='componentDisabled' placeholder="例如:nas.online、nas.com等" />
      </a-form-item>
      <a-form-item has-feedback label="主机记录" ref="subDomain" name="subDomain">
        <a-input v-model:value="formState.subDomain" :disabled='componentDisabled' placeholder="例如www、@、abc、xyz,支持多个（使用英文分号隔开，例如xyz;abc）" />
      </a-form-item>

      <a-form-item has-feedback label="AppKey" ref="AK" name="AK">
        <a-input v-model:value="formState.AK" :disabled='componentDisabled' />
      </a-form-item>
      <a-form-item has-feedback label="SecretKey" ref="SK" name="SK">
        <a-input v-model:value="formState.SK" :disabled='componentDisabled' placeholder="如果DNS服务商选择西部数码，使用域名验证则不需要填写SK" />
      </a-form-item>
      <a-form-item has-feedback label="调度周期(分钟)" ref="Cron" name="Cron">
        <a-input v-model:value="formState.Cron" :disabled='componentDisabled' placeholder="" />
      </a-form-item>
      <!-- <a-form-item label="Cron表达式">
        <a target="_blank" href="https://www.bejson.com/othertools/cron/">在线生成</a>
      </a-form-item> -->
      <a-form-item label="秘钥文档">
        <a target="_blank" :href="selectCloud.doc">{{selectCloud.value}}-查看或创建AccessKey和SecretKey </a>
      </a-form-item>
      <a-form-item :wrapper-col="{ span: 10, offset: 5 }">
        <a-space>
          <a-button type="primary" danger @click="onUpdate" v-if="componentDisabled">修改配置</a-button>
          <a-button type="primary" @click="onSubmit" v-if="!componentDisabled">确认</a-button>
          <a-button type="default" @click="onCancel" v-if="!componentDisabled">取消</a-button>
          更新成功将会立即重新执行解析任务
        </a-space>
      </a-form-item>
    </a-form>

  </a-card>
</template>
<script lang="ts" setup>
import { reactive, toRaw, ref, watch } from 'vue';
import type { UnwrapRef } from 'vue';
import { Form } from 'ant-design-vue';
import type { Rule } from 'ant-design-vue/es/form';
import type { FormInstance } from 'ant-design-vue';
import { useApiStore } from '@/store';
import { message } from 'ant-design-vue';
import { onMounted } from 'vue';
import { checkDomain, checksubDomainPrefix } from '@/utils/regexHelper';
const formRef = ref<FormInstance>();
const componentDisabled = ref(true);
const SK = ref(null);
interface FormState {
  domainServer: string;
  domain: string;
  recordType: string;
  subDomain: string;
  AK: string;
  SK: string;
  Cron: number;
  Id: string;
}
interface CloudInfo {
  key: string;
  value: string;
  doc: string;
}
const cloudNames: UnwrapRef<CloudInfo[]> = reactive([
  { key: 'aliyun', value: '阿里云', doc: 'https://ram.console.aliyun.com/manage/ak' },
  { key: 'tencent', value: '腾讯云', doc: 'https://console.cloud.tencent.com/cam/capi' },
  { key: 'huawei', value: '华为云', doc: 'https://console.huaweicloud.com/iam/#/mine/accessKey' },
  { key: 'baidu', value: '百度云', doc: 'https://console.bce.baidu.com/iam/#/iam/accesslist' },
  { key: 'jingdong', value: '京东云', doc: 'https://uc.jdcloud.com/account/accesskey' },
  { key: 'godaddy', value: 'Godaddy', doc: 'https://developer.godaddy.com/doc/endpoint/domains#/' },
  {
    key: 'west',
    value: '西部数码',
    doc: 'https://console-docs.apipost.cn/preview/bf57a993975b67e0/7b363d9b8808faa2?target_id=001',
  },
]);

const selectCloud: UnwrapRef<CloudInfo> = reactive({
  key: 'aliyun',
  value: '阿里云',
  doc: 'https://ram.console.aliyun.com/manage/ak',
});

const formState: UnwrapRef<FormState> = reactive({
  domainServer: 'aliyun',
  domain: '',
  recordType: 'A',
  subDomain: '',
  AK: '',
  SK: '',
  Cron: 30,
  Id: '0',
});

const CloudCheck = async (_rule: Rule, value: string) => {
  if (!value && formState.domainServer !== 'west') {
    return Promise.reject('请输入SecretKey');
  } else {
    formRef.value.clearValidate(['SK']);
    return Promise.resolve();
  }
};

const validateDomain = async (_rule: Rule, value: string) => {
  if (checkDomain(value)) {
    return Promise.resolve();
  } else {
    return Promise.reject('请输入正确的域名地址');
  }
};
const validateSubDomain = async (_rule: Rule, value: string) => {
  if (checksubDomainPrefix(value)) {
    return Promise.resolve();
  } else {
    return Promise.reject('请输入正确的域名前缀');
  }
};
const rules: Record<string, Rule[]> = {
  subDomain: [{ required: true, message: '请输入解析主机', trigger: 'change', validator: validateSubDomain }],
  domain: [{ required: true, message: '请输入正确的主域名', trigger: 'change', validator: validateDomain }],
  domainServer: [{ required: true, message: '请选择DNS服务商', trigger: 'change' }],
  Cron: [{ required: true, message: '请输入任务调度周期', trigger: 'change' }],
  SK: [{ required: true, validator: CloudCheck, trigger: 'change' }],
  AK: [{ required: true, message: '请输入AppKey', trigger: 'change' }],
};

//checkbox-changed
const oncouldChange = async (e) => {
  let tar = e.target.value;
  let cloud = cloudNames.find((item) => item.key === tar);
  selectCloud.doc = cloud.doc;
  selectCloud.key = cloud.key;
  selectCloud.value = cloud.value;

  try {
    const values = await formRef.value.validateFields();
    console.log('Success:', values);
  } catch (errorInfo) {
    console.log('Failed:', errorInfo);
  }
};

const getConfig = () => {
  useApiStore()
    .apiGetConfig()
    .then((res) => {
      if (res.code === 0) {
        formState.AK = res.data.ak;
        formState.domainServer = res.data.domainServer;
        formState.SK = res.data.sk;
        formState.Cron = res.data.cron;
        formState.domain = res.data.domain;
        formState.subDomain = res.data.subDomain;
        formState.recordType = res.data.recordType;
        formState.Id = res.data.id;
      } else {
        message.error(res.erro, 8);
      }
    });
};
onMounted(() => {
  getConfig();
});

//提交初始化

const onSubmit = () => {
  // console.log('submit!', toRaw(formState));
  formRef.value
    .validate()
    .then(() => {
      console.log('values', formState, toRaw(formState));
      useApiStore()
        .apiUpdateConfig(toRaw(formState))
        .then((res) => {
          if (res.code === 0) {
            message.success('配置修改生效，调度任务将按照新的规则执行');
            componentDisabled.value = true;
          } else {
            message.error(res.erro, 8);
          }
        });
    })
    .catch((error) => {
      console.log('error', error);
    });
};

//修改配置开启
const onUpdate = () => {
  componentDisabled.value = false;
};
const onCancel = () => {
  componentDisabled.value = true;
};
const labelCol = { style: { width: '150px' } };
const wrapperCol = { span: 14 };
</script>

<style lang='less' scoped>
.ant-radio-button-wrapper-disabled.ant-radio-button-wrapper-checked {
  color: rgb(164 158 158) !important;
  background-color: #e6e6e6 !important;
}
</style>