#import <Foundation/Foundation.h>
#import <Security/Security.h>
#include <string.h>
#include <stdlib.h>

static NSMutableDictionary *BeeKingdomKeychainQuery(const char *service, const char *account)
{
    NSMutableDictionary *query = [NSMutableDictionary dictionary];
    query[(__bridge id)kSecClass] = (__bridge id)kSecClassGenericPassword;
    query[(__bridge id)kSecAttrService] = [NSString stringWithUTF8String:service];
    query[(__bridge id)kSecAttrAccount] = [NSString stringWithUTF8String:account];
    return query;
}

int BeeKingdomKeychain_Set(const char *service, const char *account, const char *value)
{
    @autoreleasepool {
        if (service == NULL || account == NULL || value == NULL) return -1;

        NSData *data = [[NSString stringWithUTF8String:value] dataUsingEncoding:NSUTF8StringEncoding];
        if (data == nil) return -1;

        NSMutableDictionary *query = BeeKingdomKeychainQuery(service, account);
        SecItemDelete((__bridge CFDictionaryRef)query);

        NSMutableDictionary *attributes = [query mutableCopy];
        attributes[(__bridge id)kSecValueData] = data;
        attributes[(__bridge id)kSecAttrAccessible] = (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly;

        OSStatus status = SecItemAdd((__bridge CFDictionaryRef)attributes, NULL);
        return (int)status;
    }
}

char *BeeKingdomKeychain_Get(const char *service, const char *account)
{
    @autoreleasepool {
        if (service == NULL || account == NULL) return NULL;

        NSMutableDictionary *query = BeeKingdomKeychainQuery(service, account);
        query[(__bridge id)kSecReturnData] = @YES;
        query[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;

        CFTypeRef result = NULL;
        OSStatus status = SecItemCopyMatching((__bridge CFDictionaryRef)query, &result);
        if (status != errSecSuccess || result == NULL) return NULL;

        NSData *data = (__bridge_transfer NSData *)result;
        NSString *value = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
        if (value == nil) return NULL;

        const char *utf8 = [value UTF8String];
        return utf8 ? strdup(utf8) : NULL;
    }
}

int BeeKingdomKeychain_Delete(const char *service, const char *account)
{
    @autoreleasepool {
        if (service == NULL || account == NULL) return 0;

        NSMutableDictionary *query = BeeKingdomKeychainQuery(service, account);
        OSStatus status = SecItemDelete((__bridge CFDictionaryRef)query);
        return (status == errSecSuccess || status == errSecItemNotFound) ? 0 : (int)status;
    }
}

void BeeKingdomKeychain_FreeString(char *value)
{
    if (value != NULL) free(value);
}
