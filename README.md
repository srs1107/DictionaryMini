
# DictionaryMini
## 为什么写这么一篇博客?   
从一道亲身经历的面试题说起. 
半年前,我参加我现在所在公司的面试,面试官给了一道题,说有一个Y形的链表,知道起始节点,找出交叉节点.   
![Y形链表](https://raw.githubusercontent.com/liuzhenyulive/DictionaryMini/master/Pic/chain.gif)  
为了便于描述,我把上面的那条线路称为线路1,下面的称为线路2.    
思路1:  
先判断线路1的第一个节点的下级节点是否是线路2的第一个节点,如果不是,再判断是不是线路2的第二个,如果也不是,判断是不是第三个节点,一直到最后一个.  
如果第一轮没找到,再按以上思路处理线路一的第二个节点,第三个,第四个... 找到为止.  
时间复杂度$n^2$,相信如果我用的是这种方法,可肯定被Pass了.  
思路2:  
首先,我遍历线路2的所有节点,把节点的索引作为key,下级节点索引作为value存入字典中.
然后,遍历线路1中节点,判断字典中是否包含该节点的下级节点索引的key,即`dic.ContainsKey((node.next)`  ,如果包含,那么该下级节点就是交叉节点了.
时间复杂度是n.  
那么问题来了,面试官问我了,为什么时间复杂度n呢?你有没有研究过字典的`ContainsKey`这个方法呢?难道它不是通过遍历内部元素来判断Key是否存在的呢?如果是的话,那时间复杂度还是$n^2$才是呀?
我当时支支吾吾,说不是的,他是通过哈希表直接拿出来的,不用遍历,其实我当时也是猜的.面试官这边是敷衍过去了,但是我给自己定了一个flag,说回去一定要把这个方法研究一边,已经入职半年多了,马上要过年,想着今年的flag不能留到明年,所以写了这篇文章.  
## 带着问题来阅读  
在看这篇文章前,不知道您使用字典的时候是否有过这样的疑问.  
1. 字典为什么能无限地Add呢?
2. 从字典中取Item速度非常快,为什么呢?
3. 初始化字典可以指定字典容量,这是否多余呢?
4. 字典的桶buckets 长度为素数,为什么呢?

除了问题4,可能大家都有过,那么我们带着这些疑问,走进这篇文章.

## 从哈希函数说起
什么是哈希函数?  
哈希函数又称散列函数,是一种从任何一种数据中创建小的数字“指纹”的方法。  
下面,我们看看JDK中Sting.GetHashCode()方法.
```java
public int hashCode() {
        int h = hash;
 //hash default value : 0 
        if (h == 0 && value.length > 0) {
 //value : char storage
            char val[] = value;

            for (int i = 0; i < value.length; i++) {
                h = 31 * h + val[i];
            }
            hash = h;
        }
        return h;
    }

```
可以看到,无论多长的字符串,最终都会返回一个int值,当哈希函数确定的情况下,任何一个字符串的哈希值都是唯一且确定的.  
当然,这里只是找了一种最简单的字符数哈希值求法,理论上只要能把一个对象转换成唯一且确定值的函数,我们都可以把它称之为哈希函数.  
这是哈希函数的工作原理图.  
![哈希函数工作原理图](https://raw.githubusercontent.com/liuzhenyulive/DictionaryMini/master/Pic/HashFunction.svg?sanitize=true)  

## 字典
通过上面解释的什么是哈希函数,我们应该可以得出一个这样的结论:
`一个对象的哈希值是确定且唯一的,并求出一个对象的哈希值远比在大量数据中遍历寻找我们要的数据来得简单得多.`
那么,如何把哈希值和在集合中我们要的数据的地址关联起来呢?
如果我没有表达清楚我的意思,我们来想想这么一个场景.
如果有一天,我不小心干了什么坏事,并且警察没有逮到我本人,但是知道,这件坏事就是一个叫`阿宇`的干的,那么警察叔叔是不是第一步首先去我家里找我呢?那么警察叔叔要知道我家的地址吧?他不可能在全中国的家庭一个个去遍历,敲门,`阿宇`在不在家.
我觉得一般的套路是这样,通过我的名字,找到我的身份证号码,然后我的身份证上登记着我的家庭地址,这样来找我.

`阿宇`-----> 身份证(身份证号码,家庭住址)------>我家
我们就可以把由阿宇找到身份证号码的过程,理解为哈希函数,身份证存储着我的号码的同时,也存储着我家的地址,身份证这个角色在字典中就是 `bucket`,bucket存储着数据的内存地址(索引).
以下图片是key--->bucket的过程,也就是`阿宇`----->身份证  的过程.
![Alt text](https://raw.githubusercontent.com/liuzhenyulive/DictionaryMini/master/Pic/hashtable0.svg?sanitize=true)  

警察叔叔通过家庭住址找到了我家之后,我家除了住我,还住着我爸,我妈,警察叔叔敲门的时候,是我爸开门,于是问我爸爸,`阿宇`在哪,我爸不知道,于是我爸问我妈,`阿宇`在哪?我妈告诉警察叔叔,在书房呢.
很好,警察叔叔就这样把我给逮住了.
字典也是这样,同一个bucket可能有多个key对应,即下图中的Johon Smith和Sandra Dee,但是bucket只能记录一个内存地址(索引),也就是警察叔叔通过家庭地址找到我家时,正常来说,只有一个人过来开门,那么,如何找到也在这个家里的我的呢?我爸记录这我妈在厨房,我妈记录着我在书房,就这样,我就被揪出来了,我爸,我妈,我 就是字典中的一个entry.
![Alt text](https://raw.githubusercontent.com/liuzhenyulive/DictionaryMini/master/Pic/hashtable1.svg?sanitize=true)


![Alt text](https://raw.githubusercontent.com/liuzhenyulive/DictionaryMini/master/Pic/hashtable2.svg?sanitize=true)

![Alt text](https://raw.githubusercontent.com/liuzhenyulive/DictionaryMini/master/Pic/hashtable3.svg?sanitize=true)

