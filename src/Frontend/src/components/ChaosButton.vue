<script setup lang="ts">
import { ref } from 'vue'
import { api, ApiError } from '../api'

const emit = defineEmits<{ message: [text: string] }>()

const confirming = ref(false)
const busy = ref(false)
const notice = ref<string | null>(null)

async function kill(): Promise<void> {
  busy.value = true
  notice.value = null
  try {
    await api.killSilo()
    notice.value = 'Kill signal sent. One replica is going down.'
  } catch (error) {
    notice.value =
      error instanceof ApiError && error.status === 404
        ? 'Chaos is disabled on this host (Cluster:EnableChaos is false).'
        : error instanceof ApiError
          ? error.friendly
          : 'The kill request failed.'
  } finally {
    busy.value = false
    confirming.value = false
  }
  emit('message', notice.value ?? '')
}
</script>

<template>
  <section class="card chaos">
    <button v-if="!confirming" class="btn big" :disabled="busy" @click="confirming = true">Chaos: kill a silo</button>
    <div v-else class="confirm" role="alertdialog" aria-label="Confirm chaos">
      <p>Really kill one silo? The replica that gets the request exits immediately.</p>
      <div class="buttons">
        <button class="btn big" :disabled="busy" @click="kill">Yes, kill it</button>
        <button class="btn btn-ghost" :disabled="busy" @click="confirming = false">Cancel</button>
      </div>
    </div>
    <p v-if="notice" class="notice">{{ notice }}</p>
  </section>
</template>

<style scoped>
.chaos {
  border: 2px dashed var(--red);
}

.chaos {
  width: 20em;
  padding: 0.6em;
  background: rgb(13 17 23 / 0.9);
}

.big {
  width: 100%;
  min-height: 52px;
  font-size: 1.05em;
}

.confirm {
  display: grid;
  gap: 10px;
}

.confirm p {
  font-weight: 700;
}

.buttons {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 10px;
}

.notice {
  margin-top: 10px;
  font-weight: 800;
  color: var(--amber);
}
</style>
