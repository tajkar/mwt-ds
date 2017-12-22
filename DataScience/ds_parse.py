def json_cooked(x, do_devType=False):
    #################################
    # Optimized version based on expected structure:
    # {"_label_cost":0,"_label_probability":0.01818182,"_label_Action":9,"_labelIndex":8,"Timestamp":"2017-10-24T00:00:15.5160000Z","Version":"1","EventId":"fa68cd9a71764118a635fd3d7a908634","a":[9,11,3,1,6,4,10,5,7,8,2],"c":{"_synthetic":false,"User":{"_age":0},"Geo":{"country":"United States","_countrycf":"8","state":"New York","city":"Springfield Gardens","_citycf":"8","dma":"501"},"MRefer":{"referer":"http://www.complex.com/"},"OUserAgent":{"_ua":"Mozilla/5.0 (iPad; CPU OS 10_3_2 like Mac OS X) AppleWebKit/603.2.4 (KHTML, like Gecko) Version/10.0 Mobile/14F89 Safari/602.1","_DeviceBrand":"Apple","_DeviceFamily":"iPad","_DeviceIsSpider":false,"_DeviceModel":"iPad","_OSFamily":"iOS","_OSMajor":"10","_OSPatch":"2","DeviceType":"Tablet"},"_multi":[{"
    # Assumption: "Version" value is 1 digit
    #
    # Performance: 4x faster than Python JSON parser js = json.loads(x.strip())
    #################################
    ind1 = x.find(',',16)               # equal to: x.find(',"_label_prob',16)
    ind2 = x.find(',',ind1+23)          # equal to: x.find(',"_label_Action',ind1+23)
    ind4 = x.find(',"T',ind2+34)        # equal to: x.find(',"Timestamp',ind2+34)
    ind5 = x.find('"',ind4+36)          # equal to: x.find('","Version',ind4+36)
    ind7 = x.find('"',ind5+28)          # equal to: x.find('","a',ind5+28)
    ind8 = x.find(']',ind7+8)           # equal to: x.find('],"c',ind7+8)

    r = x[15:ind1]                      # len('{"_label_cost":') = 15
    p = x[ind1+22:ind2]                 # len(',"_label_probability":') = 22
    ts = x[ind4+14:ind5]                # len(',"Timestamp":"') = 14
    ei = x[ind5+27:ind7]                # len('","Version":"1","EventId":"') = 27
    a_vec = x[ind7+7:ind8].split(',')   # len('","a":[') = 7
    num_a = len(a_vec)
    if do_devType:
        ind9 = x.find('"DeviceType',ind8)
        if ind9 > -1:
            ind10 = x.find('"},"_mul', ind9+15)
            devType = x[ind9+14:ind10]   # len('"DeviceType":"') = 14
        else:
            devType = 'N/A'
        return ei,r,ts,float(p),int(a_vec[0]),num_a,devType
    else:
        return ei,r,ts,float(p),int(a_vec[0]),num_a

def json_dangling(x):
    #################################
    # Optimized version based on expected structure:
    # {"Timestamp":"2017-11-27T01:19:13.4610000Z","RewardValue":1.0,"EnqueuedTimeUtc":"2017-08-23T03:31:06.85Z","EventId":"d8a0391be9244d6cb124115ba33251f6"}
    #
    # Performance: 3x faster than Python JSON parser js = json.loads(x.strip())
    #################################
    ind1 = x.find('"',36)               # equal to: x.find('","RewardValue',36)
    ind2 = x.find(',',ind1+16)          # equal to: x.find(',"EnqueuedTimeUtc',ind1+16)
    ind3 = x.find('"',ind2+39)          # equal to: x.find('","EventId',ind2+39)
    ind4 = x.find('"',ind3+40)          # equal to: x.find('"}',ind3+30)

    ts = x[14:ind1]                     # len('{"Timestamp":"') = 14
    r = x[ind1+16:ind2]                 # len('","RewardValue":') = 16
    et = x[ind2+20:ind3]                # len(',"EnqueuedTimeUtc":"') = 20
    ei = x[ind3+13:ind4]                # len('","EventId":"') = 13
    return ei,r,ts,et

def extract_field(x,sep1,sep2,space=1):
    ind1 = x.find(sep1)
    ind2 = x.find(sep2,ind1+space)
    return x[ind1+len(sep1):ind2]

def local_rank(x):
    #################################
    # Optimized version based on expected structure:
    # url:https://ds-staging.microsoft.com/api/v2/marco-test-338/rank/complex_videos	status_code:200	headers:[...],"eventId":"ee9e857b57644a3fa600bc0343952ae8-sVvR","appId":"marco-test-338",[...]\n'
    #################################
    ind1 = x.find('"eventId":"')
    ind2 = x.find('"',ind1+45)
    return x[ind1+11:ind2]

def local_reward(x):
    #################################
    # Optimized version based on expected structure:
    # url:https://ds-staging.microsoft.com/api/v2/marco-test-338/reward/ee9e857b57644a3fa600bc0343952ae8-sVvR	status_code:200	headers:[...]	content:5.36
    #################################
    ind1 = x.find('/reward/')
    ind2 = x.find('\t',ind1+42)
    ind3 = x.find('\tcontent:',len(x)-35)
    return x[ind1+8:ind2],x.strip()[ind3+9:]

def cmplx_json_to_csv(input_file, output_file):
    # Used to parse cmplx dsjson lines to csv file
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write('cost,prob,city,country,state,DeviceBrand,DeviceFamily,DeviceModel,DeviceType,refer,id\n')
        i = 0
        for x in open(input_file, encoding='utf-8'):
            try:
                js = json.loads(x.strip())
                d2 = '"'+('","'.join((js['c']['OUserAgent'].get(x, 'NA').replace('"','') for x in ['_DeviceBrand', '_DeviceFamily', '_DeviceModel', 'DeviceType'])))+'"'
                d2 = d2.replace('"NA"','NA')
                if 'Geo' in js['c']:
                    d1 = '"'+('","'.join([js['c']['Geo'].get(x, 'NA') for x in ['city', 'country', 'state']]))+'"'
                    d1 = d1.replace('"NA"','NA')
                else:
                    d1 = 'NA,NA,NA'
                if 'MRefer' in js['c']:
                    d3 = '"'+js['c']['MRefer']['referer']+'"'
                else:
                    d3 = 'NA'
                d4 = '"'+js['c']['_multi'][js['_labelIndex']]['i']['id']+'"'
            except:
                print('error',x)
                continue
            f.write(str(js['_label_cost'])+','+str(js['_label_probability'])+','+','.join([d1,d2,d3,d4])+'\n')
            i += 1
            if i % 100000 == 0:
                print(i)


'''
####################################################################################################################################
# SLOW IMPLEMENTATIONS ALREADY TESTED
####################################################################################################################################

import ijson, io
def ijson_cooked(x):
    ##############################################
    # 35x slower than json_cooked with the pure Python parser
    # I'm unable to install YAJL, to use import ijson.backends.yajl2_cffi as ijson
    ##############################################
    f = io.StringIO(x.strip())
    parser = ijson.parse(f)
    i = 0
    output = []
    for prefix, event, value in parser:
        i += 1
        if prefix != None and event != 'map_key':
            output.append(value)
        if prefix == 'c':
            break
    return output[7],output[1],output[5],float(output[2]),output[3],len(output)-11

import parse
def parse_cooked(x):
    ##############################################
    # 40x slower than json_cooked with the pure Python parser
    # I'm unable to install YAJL, to use import ijson.backends.yajl2_cffi as ijson
    ##############################################
    r = parse.parse('{"_label_cost":{:n},"_label_probability":{:g},"_label_Action":{:n},"_labelIndex":{:n},"Timestamp":"{}","Version":"{:n}","EventId":"{}","a":{},{}', x)
    return r[7],r[1],r[5],r[2],r[3],len(r.fixed)-8

'''