import { IThemeAssetContributor } from '@app/shared/layout/themes/ThemeAssetContributor';
import { ThemeHelper } from '@app/shared/layout/themes/ThemeHelper';
import { DefaultThemeAssetContributor } from '@app/shared/layout/themes/default/DefaultThemeAssetContributor';
export class ThemeAssetContributorFactory {
    static getCurrent(): IThemeAssetContributor {
        let theme = ThemeHelper.getTheme();

        if (theme === 'default') {
            return new DefaultThemeAssetContributor();
        }

        return null;
    }
}
