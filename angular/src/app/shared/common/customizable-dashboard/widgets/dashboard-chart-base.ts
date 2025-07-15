export abstract class DashboardChartBase {
    loading = false;

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
