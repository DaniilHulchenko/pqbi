import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { environment } from './environments/environment';
import { hmrBootstrap } from './hmr';
import { RootModule } from './root.module';

// import { licenseKey } from 'devextreme-license';
// import config from 'devextreme/core/config';
// config({licenseKey});

if (environment.production) {
    enableProdMode();
}

const bootstrap = () => platformBrowserDynamic().bootstrapModule(RootModule);

/* "Hot Module Replacement" is enabled as described on
 * https://medium.com/@beeman/tutorial-enable-hrm-in-angular-cli-apps-1b0d13b80130#.sa87zkloh
 */

if (environment.hmr) {
    if (module['hot']) {
        hmrBootstrap(module, bootstrap); //HMR enabled bootstrap
    } else {
        console.error('HMR is not enabled for webpack-dev-server!');
        console.log('Are you using the --hmr flag for ng serve?');
    }
} else {
    bootstrap(); //Regular bootstrap
}
