<template>
  <a-form layout="inline" style="margin-top:10px;">
    <a-form-item>
      <a-range-picker v-model:value="value1" :ranges="ranges" :locale="locale" @change="datePicked" />
    </a-form-item>
    <a-form-item :wrapper-col="{ offset: 8, span: 16 }">
      <a-button type="primary" @click="GetRecords">查询</a-button>
    </a-form-item>
  </a-form>
  <a-table :columns="columns" :data-source="dataSource" bordered :pagination="pagination" @change="handleTableChange" :loading="loading">
  </a-table>
</template>
<script lang="ts" setup>
import { defineComponent, reactive, ref } from 'vue';
import { useApiStore } from '@/store';
import type { UnwrapRef } from 'vue';
import { onMounted } from 'vue';
import dayjs, { Dayjs } from 'dayjs';
import locale from 'ant-design-vue/es/date-picker/locale/zh_CN';
type RangeValue = [Dayjs, Dayjs];
import 'dayjs/locale/zh-cn';
dayjs.locale('zh-cn');
const columns = ref([
  {
    title: '时间',
    dataIndex: 'createTimeStr',
    // sorter: true,
    align: 'center',
    // width: 200,
  },
  {
    title: '解析域名',
    dataIndex: 'mainDomain',
    // sorter: true,
    align: 'center',
  },
  {
    title: '当前Ip',
    dataIndex: 'ip',
    // sorter: true,
    align: 'center',
    // width: 150,
  },
  {
    title: '上次Ip',
    dataIndex: 'lastIp',
    // sorter: true,
    align: 'center',
    // width: 150,
  },

  {
    title: '位置',
    dataIndex: 'address',
    // sorter: true,
    align: 'center',
    // width: 150,
  },
  {
    title: '运营商',
    dataIndex: 'isp',
    // sorter: true,
    align: 'center',
    // width: 150,
  },
  {
    title: 'DNS服务商',
    dataIndex: 'servr',
    // sorter: true,
    align: 'center',
    // width: 150,
  },
]);
const loading = ref(false);
interface DataItem {
  CreateTime: string;
  Address: string;
  ISP: string;
  Ip: string;
  LastIp: string;
  Servr: string;
  Id: string;
}
const datas: UnwrapRef<DataItem[]> = reactive([]);

interface quaryParam {
  dates?: string[];
  pageIndex: number;
  pageSize: number;
}

const value1 = ref<RangeValue>();
const ranges = {
  今天: [dayjs(), dayjs()] as RangeValue,
  本月: [dayjs(), dayjs().endOf('month')] as RangeValue,
};
const quaryData: UnwrapRef<quaryParam> = reactive({
  dates: null,
  pageIndex: 1,
  pageSize: 5,
});
const GetRecords = () => {
  loading.value = true;
  quaryData.pageIndex = pagination.value.current;
  quaryData.pageSize = pagination.value.defaultPageSize;
  useApiStore()
    .apiGetRecords(quaryData)
    .then((res) => {
      loading.value = false;
      if (res.code === 0) {
        dataSource.value = res.data.data;
        pagination.value.current = res.data.pageIndex;
        pagination.value.defaultPageSize = res.data.pageSize;
        pagination.value.total = res.data.total;
        pagination.value.showTotal = () => `共 ${res.data.total} 条`;
      }
    });
};
onMounted(() => {
  GetRecords();
});
const pagination = ref({
  current: 1,
  defaultPageSize: 5,
  total: 0,
  showTotal: () => `共 ${0} 条`,
});

const handleTableChange = (e) => {
  console.log(e);
  pagination.value.current = e.current;
  pagination.value.defaultPageSize = e.defaultPageSize;
  pagination.value.total = e.total;
  pagination.value.showTotal = () => `共 ${e.total} 条`;
  GetRecords();
};
const datePicked = (ref, dateArry) => {
  quaryData.dates = dateArry;
};
const dataSource = ref(datas);
</script>
<style scoped>
</style>
