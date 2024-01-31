<template>
  <a-card :bordered="false" :bodyStyle='{"padding-top":"0px","padding-bottom":"100px"}'>
    <a-form :model="formState" :label-col="labelCol" :rules="rules" :wrapper-col="wrapperCol" ref="formRef">
      <a-divider orientation="left">系统配置</a-divider>
      <!-- <a-form-item label="数据库类型" ref="DbType" name="DbType">
        <a-radio-group v-model:value="formState.DbType" button-style="solid" @change="ondbChange">
          <a-radio-button :value="item.key" :key="index" v-for="(item,index) in dbNames">{{item.value}}</a-radio-button>
        </a-radio-group>
      </a-form-item>
      <a-form-item has-feedback label="连接字符串" v-if="showDBconn.value" ref="ConnString" name="ConnString">
        <a-input v-model:value="formState.ConnString" :placeholder="selectDB.conn" />
        <a-alert style="margin-top:3px;" type="error" message="1、第一次启动:请确认数据库连接是否正确；
        2、升级后启动:可直接使用原数据库配置信息初始化"></a-alert>
      </a-form-item>
      <a-form-item label="" style="margin-left:100px;" v-if="formState.DbType!==2">

        <a-space wrap>
          <a-button type="primary" block @click="checkConnValidity">测试连接</a-button>
          <a-alert type="success" :message="selectDB.conn"></a-alert>
        </a-space>

      </a-form-item>  v-if="isFirst||formState.DbType===2"-->
      <div>
        <a-form-item has-feedback label="管理员账号" ref="UserName" name="UserName">
          <a-input v-model:value="formState.UserName" />
        </a-form-item>
        <a-form-item has-feedback label="管理员密码" ref="UswePwd" name="UswePwd">
          <a-input v-model:value="formState.UswePwd" />
        </a-form-item>
        <a-form-item has-feedback label="调度周期(分钟)" ref="Cron" name="Cron">
          <a-input v-model:value="formState.Cron" placeholder="" />
        </a-form-item>
        <!-- <a-form-item label="Cron表达式">
          <a target="_blank" href="https://www.bejson.com/othertools/cron/">在线生成</a>
        </a-form-item> -->
        <a-divider orientation="left">DNS配置</a-divider>
        <a-form-item label="DNS服务商" ref="cloudName" name="cloudName">
          <a-radio-group v-model:value="formState.cloudName" button-style="solid" @change="oncouldChange">
            <a-radio-button :value="item.key" :key="index" v-for="(item,index) in cloudNames">{{item.value}}</a-radio-button>
          </a-radio-group>
        </a-form-item>

        <a-form-item has-feedback label="主域名" ref="domainName" name="domainName">
          <a-input v-model:value="formState.domainName" placeholder="例如:nas.online、nas.com等" />
        </a-form-item>
        <a-form-item has-feedback label="主机记录" ref="domainRecord" name="domainRecord">
          <a-input v-model:value="formState.domainRecord" placeholder="例如www、@、abc、xyz,支持多个（使用英文分号隔开，例如xyz;abc）" />
        </a-form-item>

        <a-form-item has-feedback label="AppKey" ref="AK" name="AK">
          <a-input v-model:value="formState.AK" />
        </a-form-item>
        <a-form-item has-feedback label="SecretKey" ref="SK" name="SK">
          <a-input v-model:value="formState.SK" placeholder="如果DNS服务商选择西部数码，使用域名验证则不需要填写SK" />
        </a-form-item>
        <a-form-item label="秘钥文档">
          <a target="_blank" :href="selectCloud.doc">{{selectCloud.value}}-查看或创建AccessKey和SecretKey </a>
        </a-form-item>
        <a-form-item :wrapper-col="{ span: 10, offset: 5 }">
          <a-button type="primary" danger @click="onSubmit">开始初始化系统</a-button>
        </a-form-item>
      </div>
    </a-form>
  </a-card>
</template>
<script lang="ts" setup>
import { reactive, toRaw, ref, watch, createVNode, h } from 'vue';
import type { UnwrapRef } from 'vue';
import { Form } from 'ant-design-vue';
import type { Rule } from 'ant-design-vue/es/form';
import type { FormInstance } from 'ant-design-vue';
import { useApiStore } from '@/store';
import { Modal } from 'ant-design-vue';
import { checkDomain, checksubDomainPrefix, checkPass, checkUserName } from '@/utils/regexHelper';
const formRef = ref<FormInstance>();
const SK = ref(null);
interface FormState {
  cloudName: string;
  domainName: string;
  recordType: string;
  domainRecord: string;
  AK: string;
  SK: string;
  DbType: number;
  ConnString: string;
  Cron: number;
  UserName: string;
  UswePwd: string;
}
interface CloudInfo {
  key: string;
  value: string;
  doc: string;
}
interface DbTypeInfo {
  key: number;
  value: string;
  conn: string;
}
interface showDBconn {
  value: boolean;
}

const showDBconn: UnwrapRef<showDBconn> = reactive({
  value: false,
});
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

const selectDB: UnwrapRef<DbTypeInfo> = reactive({
  key: 2,
  value: 'Sqlite',
  conn: '',
});

const dbNames: UnwrapRef<DbTypeInfo[]> = reactive([
  { key: 2, value: 'Sqlite', conn: '' },
  { key: 0, value: 'MySql', conn: 'Server=host;Port=3306;Database=dbname;Uid=root;Pwd=123456;' },
  { key: 1, value: 'SqlServer', conn: 'Server=host;Database=dbname;User ID=sa;Password=123456;' },
  {
    key: 4,
    value: 'PostgreSQL',
    conn: 'PORT=5432;DATABASE=dbname;HOST=localhost;PASSWORD=123456;USER ID=userid;MaxPoolSize=512',
  },
]);

const selectCloud: UnwrapRef<CloudInfo> = reactive({
  key: 'aliyun',
  value: '阿里云',
  doc: 'https://ram.console.aliyun.com/manage/ak',
});

const formState: UnwrapRef<FormState> = reactive({
  cloudName: 'aliyun',
  domainName: '',
  recordType: 'A',
  domainRecord: '',
  AK: '',
  SK: '',
  DbType: 2,
  ConnString: '',
  Cron: 30,
  UserName: '',
  UswePwd: '',
});

//验证
const ConnStringCheck = async (_rule: Rule, value: string) => {
  if (!value && formState.DbType !== 2) {
    return Promise.reject('请输入有效的数据库连接字符串');
  } else {
    formRef.value.clearValidate(['ConnString']);
    return Promise.resolve();
  }
};
const CloudCheck = async (_rule: Rule, value: string) => {
  if (!value && formState.cloudName !== 'west') {
    return Promise.reject('请输入云NDS平台-SecretKey');
  } else {
    formRef.value.clearValidate(['SK']);
    return Promise.resolve();
  }
};

const validateDomain = async (_rule: Rule, value: string) => {
  if (checkDomain(value)) {
    return Promise.resolve();
  } else {
    return Promise.reject('请输入有效的域名');
  }
};
const validateSubDomain = async (_rule: Rule, value: string) => {
  if (checksubDomainPrefix(value)) {
    return Promise.resolve();
  } else {
    return Promise.reject('请输入有效的域名前缀');
  }
};
const validateUserPass = async (_rule: Rule, value: string) => {
  if (checkPass(value)) {
    return Promise.resolve();
  } else {
    return Promise.reject('请输入有效的密码（至少8位，包含大小写以及数字）');
  }
};
const validateUserName = async (_rule: Rule, value: string) => {
  if (checkUserName(value)) {
    return Promise.resolve();
  } else {
    return Promise.reject('请输入有效的用户名（不能有特殊字符，至少3位及以上）');
  }
};

const rules: Record<string, Rule[]> = {
  UserName: [
    {
      required: true,
      message: '请输入有效的用户名（不能有特殊字符，至少3位及以上）',
      trigger: 'change',
      validator: validateUserName,
    },
  ],
  UswePwd: [
    {
      required: true,
      message: '请输入有效的密码（至少8位，包含大小写以及数字）',
      trigger: 'change',
      validator: validateUserPass,
    },
  ],
  DbType: [{ required: true, message: '请选择数据库类型', trigger: 'change' }],
  ConnString: [{ required: true, validator: ConnStringCheck, trigger: 'change' }],
  domainRecord: [{ required: true, message: '请输入有效的域名前缀', trigger: 'change', validator: validateSubDomain }],
  domainName: [{ required: true, message: '请输入有效的域名', trigger: 'change', validator: validateDomain }],
  cloudName: [{ required: true, message: '请选择DNS服务商', trigger: 'change' }],
  Cron: [{ required: true, message: '请输入任务调度周期', trigger: 'change' }],
  SK: [{ required: true, validator: CloudCheck, trigger: 'change' }],
  AK: [{ required: true, message: '请输入云NDS平台-AppKey', trigger: 'change' }],
};

watch(
  () => formState.DbType,
  () => {
    formRef.value.validateFields(['ConnString']);
  },
  { flush: 'post' }
);
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
const ondbChange = async (e) => {
  let tar = e.target.value;
  let db = dbNames.find((item) => item.key === tar);
  showDBconn.value = tar !== 2;
  selectDB.key = db.key;
  selectDB.value = db.value;
  selectDB.conn = db.conn;
  try {
    const values = await formRef.value.validateFields();
    console.log('Success:', values);
  } catch (errorInfo) {
    console.log('Failed:', errorInfo);
  }
};

//检查数据库连接
import { message } from 'ant-design-vue';
import router from '@/router';
import { ExclamationCircleOutlined } from '@ant-design/icons-vue';
const isFirst = ref(false);

const checkConnValidity = async () => {
  useApiStore()
    .apiCheckDB(formState.DbType, formState.ConnString)
    .then((res) => {
      // console.log(res);
      if (res.code === 0) {
        if (res.data === 1) {
          // isFirst.value = false;
          // message.success('原数据库原配置可用，可选择直接登录或重新初始化', 5);
          showConfirm();
        } else {
          isFirst.value = true;
          message.success('恭喜，数据库连接有效！');
        }
      } else {
        message.error(res.erro, 8);
      }
    });
};

const showConfirm = () => {
  Modal.confirm({
    title: '友情提醒',
    icon: createVNode(ExclamationCircleOutlined),
    content: '数据库存在且已有配置，可直接登录或重新初始化',
    okText: '重新初始化(清除数据)',
    okType: 'danger',
    cancelText: '直接登录',
    onOk() {
      // console.log('重新初始化');
      isFirst.value = true;
    },
    onCancel() {
      isFirst.value = false;
      useApiStore()
        .apiUseOldAppConfig(formState.DbType, formState.ConnString)
        .then((res) => {
          if (res.code === 0) {
            setTimeout(() => {
              router.push('/login');
            }, 1000);
          } else {
            message.error(res.erro, 8);
          }
        });
    },
  });
};

//提交初始化

const onSubmit = () => {
  // console.log('submit!', toRaw(formState));
  formRef.value
    .validate()
    .then(() => {
      console.log('values', formState, toRaw(formState));
      useApiStore()
        .apiInit(toRaw(formState))
        .then((res) => {
          if (res.code === 0) {
            message.success('初始化成功');
            setTimeout(() => {
              router.push('/login');
            }, 1000);
          } else {
            if (res.data === 'dbexist') {
            }
            message.error(res.erro, 8);
          }
        });
    })
    .catch((error) => {
      console.log('error', error);
    });
};

const labelCol = { style: { width: '150px' } };
const wrapperCol = { span: 14 };
</script>
<style scoped>
.ant-card-body {
  padding: 5px !important;
}
</style>

