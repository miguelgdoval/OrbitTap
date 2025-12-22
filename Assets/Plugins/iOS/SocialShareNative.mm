#import <UIKit/UIKit.h>

extern "C" {
    void _ShareText(const char* text) {
        NSString *shareText = [NSString stringWithUTF8String:text];
        
        UIViewController *rootViewController = [UIApplication sharedApplication].keyWindow.rootViewController;
        
        // Si no hay rootViewController, intentar obtenerlo de otra forma
        if (rootViewController == nil) {
            UIWindow *window = [UIApplication sharedApplication].windows.firstObject;
            if (window != nil) {
                rootViewController = window.rootViewController;
            }
        }
        
        if (rootViewController == nil) {
            return; // No se puede compartir sin un view controller
        }
        
        // Crear UIActivityViewController
        UIActivityViewController *activityViewController = 
            [[UIActivityViewController alloc] initWithActivityItems:@[shareText] 
                                              applicationActivities:nil];
        
        // Excluir algunas actividades si lo deseas (opcional)
        // activityViewController.excludedActivityTypes = @[UIActivityTypeAirDrop];
        
        // Para iPad, necesitamos un popover
        if (UI_USER_INTERFACE_IDIOM() == UIUserInterfaceIdiomPad) {
            activityViewController.popoverPresentationController.sourceView = rootViewController.view;
            activityViewController.popoverPresentationController.sourceRect = 
                CGRectMake(rootViewController.view.bounds.size.width / 2, 
                          rootViewController.view.bounds.size.height / 2, 0, 0);
            activityViewController.popoverPresentationController.permittedArrowDirections = 0;
        }
        
        // Presentar el selector de compartir
        dispatch_async(dispatch_get_main_queue(), ^{
            [rootViewController presentViewController:activityViewController 
                                             animated:YES 
                                           completion:nil];
        });
    }
}

