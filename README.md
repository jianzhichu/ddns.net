# ddns.net
程序使用.NetCore6.0开发（前后端SPA模式）。

支持阿里云，腾讯云，百度云，华为云，京东云，Godaddy、西部数码，支持IPV4,IPV6。

相比DDNS-GO并没什么优势（反而更吃内存：1、技术问题，2、NetCore框架吃内存），有兴趣可以试一下。

没有开源计划（都是调用接口，无技术含量，开源意义不大）。

IPV4通过接口获取（https://www.cz88.net）

IPV6优先网卡获取，其次接口获取（https://6.ipw.cn,https://speed.neu6.edu.cn/getIP.php，https://v6.ident.me）

1、安装:直接 docker run

```
docker run -it -e ASPNETCORE_URLS=http://*:8888 -d --restart=always --net=host --name ddns.net deathvicky/ddns.net
```
参数说明

-e ASPNETCORE_URLS=http://*:8888 指定服务地址
--net=host  为了获取宿主机网卡IPV6地址，不加则默认使用bridge 无法获取IPV6地址
1.1 如果不需要IPV6或者说家里没有IPV6 则使用下面的命令即可

```
docker run -it -d --restart=always -p 8888:3344 --name ddns.net deathvicky/ddns.net
```
2、运行+配置

浏览器访问 http://ip:8888

填写相应的数据库配置，账号配置，域名配置

![image](https://github.com/jianzhichu/ddns.net/assets/13816657/41833c96-579d-4dba-8205-ad3386b00be1)

