import { defineStore, storeToRefs } from 'pinia';
import http from './http';
import { ref, watch } from 'vue';
import { Response } from '@/types';
// import { RouteOption } from '@/router/interface';
// import { addRoutes, removeRoute } from '@/router/dynamicRoutes';
// import { useSettingStore } from './setting';
// import { RouteRecordRaw, RouteMeta } from 'vue-router';
// import { useAuthStore } from '@/plugins';
// import router from '@/router';

// export interface MenuProps {
//   id?: number;
//   name: string;
//   path: string;
//   title?: string;
//   icon?: string;
//   badge?: number | string;
//   target?: '_self' | '_blank';
//   link?: string;
//   component: string;
//   renderMenu?: boolean;
//   permission?: string;
//   parent?: string;
//   children?: MenuProps[];
//   cacheable?: boolean;
//   view?: string;
// }

export const useApiStore = defineStore('coreapi', () => {
  //检查是否已经初始化过了
  async function apiCheckInitStatus() {
    const initStatus = localStorage.getItem('ddns-init');
    if (initStatus && initStatus === '1') {
      return { code: 0 }
    } else {
      return http
        .request<any, Response<any>>('/api/init/Check', 'GET')
        .then((res) => {
          // console.log(res)
          if (res.data.code === 0) {
            // localStorage.setItem('ddns-init', '1')
          }
          return res.data;
        })
        .finally(() => {

        });
    }
  }
  //检查数据库连接
  async function apiCheckDB(DbType: number, ConnString: string) {
    return http
      .request<any, Response<any>>('/api/init/CheckConnString', 'post_json', { DbType, ConnString })
      .then((res) => {
        // console.log(res)
        return res.data;
      })
      .finally(() => {

      });
  }
  //初始化系统配置
  async function apiInit(request: object) {
    console.log(request)
    return http
      .request<any, Response<any>>('/api/init/Init', 'post_json', request)
      .then((res) => {
        // console.log(res)

        return res.data;
      })
      .finally(() => {

      });
  }
  //获取配置
  async function apiGetConfig() {
    return http
      .request<any, Response<any>>('/api/sys/GetConfig', 'GET')
      .then((res) => {
        console.log(res)
        return res.data;
      })
      .finally(() => {

      });
  }
  //修改配置
  async function apiUpdateConfig(request: object) {
    return http
      .request<any, Response<any>>('/api/init/SaveDomainConfig', 'post_json', request)
      .then((res) => {
        console.log(res)
        return res.data;
      })
      .finally(() => {

      });
  }
  //修改配置
  async function apiGetRecords(request: object) {
    return http
      .request<any, Response<any>>('/api/domain/GetAnalysisRecords', 'post_json', request)
      .then((res) => {
        // console.log(res)
        return res.data;
      })
      .finally(() => {

      });
  }
  //后台日志
  async function apiGetLogs(path: string) {
    return http.request<any, Response<any>>(path, 'get').then(r => {
      // console.log(r)
      return r.data;
    }).finally(() => {

    });
  }
  //用户信息-头像
  async function apiUserInfo() {
    return http.request<any, Response<any>>('/api/sys/GetUserAvatar', 'get').then(r => {
      return r.data;
    }).finally(() => {

    });
  }
  //密码修改
  async function apiChangePwd(param: object) {
    return http.request<any, Response<any>>('/api/sys/UpdatePwd', 'post_json', param).then(r => {
      return r.data;
    }).finally(() => {

    });
  }

  //获取stmp配置
  async function apiGetStmpConfig() {
    return http.request<any, Response<any>>('/api/sys/GetStmpConfig', 'get').then(r => {
      return r.data;
    }).finally(() => {

    });
  }
  //设置stmp配置
  async function apiSaveStmpConfig(params: any) {
    return http.request<any, Response<any>>('/api/sys/SaveStmpConfig', 'post_json', params).then(r => {
      return r.data;
    }).finally(() => {

    });
  }
  //升级或重新构建后使用原来的数据库配置
  async function apiUseOldAppConfig(DbType: number, ConnString: string) {
    return http.request<any, Response<any>>('/api/init/UseOldAppConfig', 'post_json', { DbType, ConnString }).then(r => {
      return r.data;
    }).finally(() => {

    });
  }
  return {
    apiCheckInitStatus,
    apiCheckDB,
    apiInit,
    apiGetConfig,
    apiUpdateConfig,
    apiGetRecords,
    apiGetLogs,
    apiUserInfo,
    apiChangePwd,
    apiGetStmpConfig,
    apiSaveStmpConfig,
    apiUseOldAppConfig
  };
});
