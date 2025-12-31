export abstract class DashboardChartBase {
    loading = false;
    isInitialLoad = false;

    showLoading() {
        setTimeout(() => {
            this.loading = true;
        });
    }

    hideLoading() {
        setTimeout(() => {
            this.loading = false;
        });
    }
}
