import { NavigationGuard, NavigationHookAfter } from 'vue-router';
import http from '@/store/http';
import { useAccountStore, useMenuStore, useApiStore } from '@/store';
import { useAuthStore } from '@/plugins';
import NProgress from 'nprogress';
import 'nprogress/nprogress.css';
import router from '@/router';

NProgress.configure({ showSpinner: false });

interface NaviGuard {
  before?: NavigationGuard;
  after?: NavigationHookAfter;
}

const loginGuard: NavigationGuard = function (to, from) {
  // console.log('Authorization', http.checkAuthorization())
  const account = useAccountStore();
  if (!http.checkAuthorization() && !/^\/(login|home|init)?$/.test(to.fullPath)) {
    account.setLogged(false)
    return '/login';
  } else {
  }

};

const dynamicinitRoute =
{
  path: '/',
  name: 'init',
  redirect: '/init',
  meta: {
    title: '初始化',
    renderMenu: false,
    icon: 'CreditCardOutlined',
  },
  children: null,
  component: () => import('@/pages/init'),
};


const InitGuard: NavigationGuard = function (to, from) {

  if (to.fullPath != '/init') {
    useApiStore()
      .apiCheckInitStatus()
      .then((res) => {
        // console.log(to.fullPath)
        if (res.code === 0) {
        } else {
          if (!router.hasRoute('init')) {
            router.addRoute(dynamicinitRoute)
          }
          router.push('/init')
          // return '/init'
        }
      });
  }
};


// 进度条
const ProgressGuard: NaviGuard = {
  before(to, from) {
    NProgress.start();
  },
  after(to, from) {
    NProgress.done();
  },
};

const AuthGuard: NaviGuard = {
  before(to, from) {
    const { hasAuthority } = useAuthStore();
    if (to.meta?.permission && !hasAuthority(to.meta?.permission)) {
      return { name: '403', query: { permission: to.meta.permission, path: to.fullPath } };
    }
  },
};

const ForbiddenGuard: NaviGuard = {
  before(to) {
    if (to.name === '403' && (to.query.permission || to.query.path)) {
      to.fullPath = to.fullPath
        .replace(/permission=[^&=]*&?/, '')
        .replace(/&?path=[^&=]*&?/, '')
        .replace(/\?$/, '');
      to.params.permission = to.query.permission;
      to.params.path = to.query.path;
      delete to.query.permission;
      delete to.query.path;
    }
  },
};

// 404 not found
const NotFoundGuard: NaviGuard = {
  before(to, from) {
    const { loading } = useMenuStore();
    if (to.meta._is404Page && loading) {
      to.params.loading = true as any;
    }
  },
};

export default {
  before: [ProgressGuard.before, InitGuard, loginGuard, AuthGuard.before, ForbiddenGuard.before, NotFoundGuard.before],
  after: [ProgressGuard.after],
};
